using CoreRCON;
using CoreRCON.PacketFormats;
using CoreRCON.Parsers.Standard;
using kTVCSS.Models;
using kTVCSS.Settings;
using kTVCSS.Tools;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;
using MySqlConnector;
using static kTVCSS.Game.Sourcemod;
using System.Timers;

namespace kTVCSS
{
    class Program
    {
        public static Logger Logger = new Logger(0);
        public static ConfigTools ConfigTools = new ConfigTools();
        public static List<Server> Servers = new List<Server>();
        public static List<string> ForbiddenWords = new List<string>();

        public class Node
        {
            public Node()
            {
                alertThread = new Thread(Alerter) { IsBackground = true };
            }

            private static Thread alertThread = null;
            private RCON rcon = null;
            private Match match = new Match(0);
            private List<string> mapQueue = new List<string>();
            private Dictionary<int, string> mapPool = new Dictionary<int, string>();
            private static System.Timers.Timer aTimer;

            public static FTPTools FTPTools = null;
            public List<Player> MatchPlayers = null;
            public static List<PlayerRank> PlayersRank = new List<PlayerRank>();
            public static List<Player> OnlinePlayers = new List<Player>();
            public static int ServerID = 0;
            public static bool NeedRestart = false;
            public static string DemoName = string.Empty;

            private bool isCanBeginMatch = true;
            private bool isResetFreezeTime = false;
            private bool isBestOfOneStarted = false;
            private bool isBestOfThree = false;
            private string currentMapSelector = string.Empty;
            private string terPlayerSelector = string.Empty;
            private string ctPlayerSelector = string.Empty;
            private string tName = "TERRORIST";
            private string ctName = "CT";
            private string currentMapName = string.Empty;
            
            public async Task StartNode(Server server)
            {
                Logger.LoggerID = server.ID;
                ServerID = server.ID;
                SetAutoRestartTimer();
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(server.Host), server.GamePort);
                rcon = new RCON(endpoint, server.RconPassword);
                rcon.OnDisconnected += Rcon_OnDisconnected;
                bool isRconConnectionEstablished = false;
                while (!isRconConnectionEstablished)
                {
                    try
                    {
                        await rcon.ConnectAsync();
                        isRconConnectionEstablished = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Print(server.ID, $"{ex.Message}", LogLevel.Error);
                        Thread.Sleep(30000);
                    }
                }
                LogReceiver log = new LogReceiver(server.NodePort, endpoint);
                ServerQueryPlayer[] players = await ServerQuery.Players(endpoint);
                var checkList = players.ToList();
                checkList.RemoveAll(x => x.Duration == -1);
                SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                FTPTools = new FTPTools(server);
                Logger.Print(server.ID, $"Created connection to {info.Name}", LogLevel.Trace);
                await RconHelper.SendMessage(rcon, "Соединение до центрального сервера kTVCSS установлено!", Colors.ivory);
#if DEBUG
                await RconHelper.SendMessage(rcon, "PROCESS STARTED IN DEBUG MODE", Colors.crimson);
#endif
                alertThread.Start(server);

                var recoveredMatchID = await MatchEvents.CheckMatchLiveExists(server.ID);
                if (checkList.Count > 0 && recoveredMatchID == 0)
                {
                    await RconHelper.SendCmd(rcon, "sm_map " + info.Map);
                }

                log.Listen<KillFeed>(async kill =>
                {
                    if (match.IsMatch)
                    {
                        int hs = 0;
                        if (kill.Headshot)
                            hs = 1;

                        if (kill.Killer.Team == kill.Killed.Team)
                        {
                            return;
                        }

                        if (!match.PlayerKills.ContainsKey(kill.Killer.SteamId))
                        {
                            match.PlayerKills.Add(kill.Killer.SteamId, 1);
                            await ServerEvents.AuthPlayer(kill.Killer.SteamId, kill.Killer.Name);
                        }
                        else
                        {
                            match.PlayerKills[kill.Killer.SteamId]++;
                        }

                        await MatchEvents.PlayerKill(kill.Killer.Name, kill.Killed.Name, kill.Killer.SteamId, kill.Killed.SteamId, hs, server.ID, match.MatchId);

                        if (!MatchPlayers.Where(x => x.SteamId == kill.Killer.SteamId).Any()) MatchPlayers.Add(kill.Killer);
                        else
                        {
                            MatchPlayers.Where(x => x.SteamId == kill.Killer.SteamId).First().Name = kill.Killer.Name;
                            MatchPlayers.Where(x => x.SteamId == kill.Killer.SteamId).First().Team = kill.Killer.Team;
                        }
                        if (!MatchPlayers.Where(x => x.SteamId == kill.Killed.SteamId).Any()) MatchPlayers.Add(kill.Killed);
                        else
                        {
                            MatchPlayers.Where(x => x.SteamId == kill.Killed.SteamId).First().Name = kill.Killed.Name;
                            MatchPlayers.Where(x => x.SteamId == kill.Killed.SteamId).First().Team = kill.Killed.Team;
                        }

                        if (match.OpenFragSteamID == string.Empty)
                        {
                            match.OpenFragSteamID = kill.Killer.SteamId;
                        }
                        await MatchEvents.WeaponKill(kill.Killer.SteamId, kill.Weapon);
                        await MatchEvents.InsertMatchLog(match.MatchId, $"{kill.Killer.Name}<{kill.Killer.SteamId}> killed" +
                            $" {kill.Killed.Name}<{kill.Killed.SteamId}> with weapon <{kill.Weapon}> <{kill.Headshot}>", info.Map, server.ID);
                    }
                });

                log.Listen<RoundStart>(async result =>
                {
                    if (match.IsMatch)
                    {
                        await RconHelper.SendCmd(rcon, "save_match");

                        match.PlayerKills.Clear();
                        match.OpenFragSteamID = string.Empty;

                        if (isResetFreezeTime)
                        {
                            match.IsNeedSetTeamScores = true;
                            await RconHelper.LiveOnThree(rcon, match, OnlinePlayers);
                            isResetFreezeTime = !isResetFreezeTime;
                        }

                        if (match.Pause)
                        {
                            await RconHelper.SendCmd(rcon, "mp_freezetime 10");
                            match.Pause = !match.Pause;
                        }

                        await MatchEvents.InsertMatchLog(match.MatchId, $"<Round Start>", info.Map, server.ID);
                        
                        if (match.AScore == 0 && match.BScore == 0)
                        {
                            IEnumerable<Player> ters = MatchPlayers.Where(x => x.Team == tName);
                            IEnumerable<Player> cts = MatchPlayers.Where(x => x.Team == ctName);
                            int terAvg = 0;
                            int terCount = 0;
                            int ctAvg = 0;
                            int ctCount = 0;
                            try 
                            {
                                foreach (var player in ters) 
                                {
                                    int pts = PlayersRank.Where(x => x.SteamID == player.SteamId).First().Points;
                                    if (pts != 0)
                                    {
                                        terAvg += pts;
                                        terCount++;
                                    }
                                }

                                foreach (var player in cts) 
                                {
                                    int pts = PlayersRank.Where(x => x.SteamID == player.SteamId).First().Points;
                                    if (pts != 0)
                                    {
                                        ctAvg += pts;
                                        ctCount++;
                                    }
                                }

                                if (terCount != 0)
                                {
                                    double ter = double.Parse(terAvg.ToString()) / double.Parse(terCount.ToString());
                                    ter = Math.Round(ter);
                                    if (ter != 0)
                                    {
                                        if (ctCount != 0)
                                        {
                                            double ct = double.Parse(ctAvg.ToString()) / double.Parse(ctCount.ToString());
                                            ct = Math.Round(ct);
                                            if (ct != 0)
                                            {
                                                var tags = MatchEvents.GetTeamNames(MatchPlayers);
                                                await RconHelper.SendMessage(rcon, $"Средний рейтинг команды {tags[tName]} - {ter}", Colors.crimson);
                                                await RconHelper.SendMessage(rcon, $"Средний рейтинг команды {tags[ctName]} - {ct}", Colors.dodgerblue);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception) 
                            {

                            }
                        }
                    }
                });

                //log.Listen<RestartRound>(async result =>
                //{
                    
                //});

                log.Listen<RoundEndScore>(async result =>
                {
                    if (match.IsMatch)
                    {
                        await RconHelper.SendCmd(rcon, "save_match");

                        if (result.WinningTeam == tName)
                        {
                            match.AScore += 1;
                            if (match.IsOvertime)
                                match.AScoreOvertime += 1;
                        }
                        if (result.WinningTeam == ctName)
                        {
                            match.BScore += 1;
                            if (match.IsOvertime)
                                match.BScoreOvertime += 1;
                        }

                        SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                        var tags = MatchEvents.GetTeamNames(MatchPlayers);

                        await MatchEvents.InsertMatchLog(match.MatchId, $"<Round End> {tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}", info.Map, server.ID);

                        match.RoundID++;

                        foreach (var player in match.PlayerKills)
                        {
                            if (match.OpenFragSteamID == player.Key)
                            {
                                await MatchEvents.SetMatchHighlight(player.Key, player.Value, match.MatchId, true);
                            }
                            else
                            {
                                await MatchEvents.SetMatchHighlight(player.Key, player.Value, match.MatchId, false);
                            }

                            if (player.Value >= 3)
                            {
                                await MatchEvents.SetHighlight(player.Key, player.Value);
                                switch (player.Value)
                                {
                                    case 3:
                                        {
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a triple kill!", Colors.mediumseagreen);
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a triple kill!", info.Map, server.ID);
                                            break;
                                        }
                                    case 4:
                                        {
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a quad kill!", Colors.legendary);
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a quad kill!", info.Map, server.ID);
                                            break;
                                        }
                                    case 5:
                                        {
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!", Colors.crimson);
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!", info.Map, server.ID);
                                            break;
                                        }
                                }
                            }
                        }
                        await MatchEvents.SetOpenFrag(match.OpenFragSteamID);
                        await MatchEvents.UpdateMatchScore(match.AScore, match.BScore, server.ID, match.MatchId);
                        await RconHelper.SendCmd(rcon, "sys_say {crimson}" + tags[tName] + " {ivory}[" + match.AScore + "-" + match.BScore + "]{dodgerblue} " + tags[ctName]);

                        #region Обработка результатов матча

                        #region Если не оверы
                        if (!match.IsOvertime)
                        {
                            #region Смена сторон после первой половины

                            if (match.AScore + match.BScore == match.MaxRounds)
                            {
                                await RconHelper.SendCmd(rcon, "sm_msay " + $"{tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\nСейчас будет установлен тайм-аут на одну минуту.\\nМатч продолжится автоматически.");
                                await RconHelper.SendMessage(rcon, "Тайм-аут!", Colors.crimson);
                                await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                                Thread.Sleep(2000);
                                await RconHelper.SendCmd(rcon, "sm_swap @all");
                                await RconHelper.SendCmd(rcon, "sv_pausable 1");
                                await RconHelper.SendMessage(rcon, "Одна минута перерыва!", Colors.crimson);
                                isResetFreezeTime = true;
                                match.FirstHalf = false;
                                var _aScore = match.AScore;
                                var _bScore = match.BScore;
                                var _AOScore = match.AScoreOvertime;
                                var _BOScore = match.BScoreOvertime;
                                match.AScore = _bScore;
                                match.BScore = _aScore;
                                match.AScoreOvertime = _BOScore;
                                match.BScoreOvertime = _AOScore;
                                foreach (Player player in MatchPlayers)
                                {
                                    if (player.Team == tName)
                                    {
                                        player.Team = ctName;
                                    }
                                    else
                                    {
                                        player.Team = tName;
                                    }
                                }
                            }

                            #endregion

                            #region Окончание матча или начало оверов

                            if (!match.FirstHalf)
                            {
                                if ((Math.Abs(match.AScore - match.BScore) >= 2) && (match.AScore == match.MaxRounds + 1 || match.BScore == match.MaxRounds + 1))
                                {
                                    await OnEndMatch(tags, result.WinningTeam, info, server);

                                    if (mapQueue.Count > 0 && isBestOfThree)
                                    {
                                        await RconHelper.SendMessage(rcon, $"Автоматическая смены карты на {mapQueue.FirstOrDefault().Trim()} через минуту!", Colors.legendary);
                                        Thread.Sleep(60000);
                                        await RconHelper.SendCmd(rcon, $"changelevel {mapQueue.FirstOrDefault().Trim()}");
                                        mapQueue.Remove(mapQueue.FirstOrDefault());
                                    }
                                    else if (mapQueue.Count == 0)
                                    {
                                        isBestOfThree = false;
                                    }
                                }

                                if (match.AScore + match.BScore == match.MaxRounds * 2)
                                {
                                    if (match.IsMatch)
                                    {
                                        match.AScoreOvertime = 0;
                                        match.BScoreOvertime = 0;
                                        match.IsOvertime = true;
                                        await RconHelper.SendMessage(rcon, "Овертайм!!!", Colors.crimson);
                                        await RconHelper.SendCmd(rcon, "sv_pausable 1");
                                        await RconHelper.SendCmd(rcon, "mp_freezetime 30");
                                        Thread.Sleep(2000);
                                        await RconHelper.SendMessage(rcon, "Полминуты перерыва!", Colors.crimson);
                                        isResetFreezeTime = true;
                                        match.MaxRounds = 3;
                                    }
                                }
                            }

                            #endregion
                        }
                        #endregion
                        #region Оверы
                        else
                        {
                            #region Смена сторон в оверах

                            if (match.AScoreOvertime + match.BScoreOvertime == match.MaxRounds && match.AScoreOvertime != match.BScoreOvertime)
                            {
                                await RconHelper.SendCmd(rcon, "sm_msay " + $"{tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\nСейчас будет установлен тайм-аут на одну минуту.\\nМатч продолжится автоматически.");
                                await RconHelper.SendMessage(rcon, "Тайм-аут!", Colors.crimson);
                                await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                                Thread.Sleep(2000);
                                await RconHelper.SendCmd(rcon, "sm_swap @all");
                                await RconHelper.SendCmd(rcon, "sv_pausable 1");
                                await RconHelper.SendMessage(rcon, "Одна минута перерыва!", Colors.crimson);
                                isResetFreezeTime = true;
                                match.FirstHalf = false;
                                var _aScore = match.AScore;
                                var _bScore = match.BScore;
                                var _AOScore = match.AScoreOvertime;
                                var _BOScore = match.BScoreOvertime;
                                match.AScore = _bScore;
                                match.BScore = _aScore;
                                match.AScoreOvertime = _BOScore;
                                match.BScoreOvertime = _AOScore;
                                foreach (Player player in MatchPlayers)
                                {
                                    if (player.Team == tName)
                                    {
                                        player.Team = ctName;
                                    }
                                    else
                                    {
                                        player.Team = tName;
                                    }
                                }
                            }

                            #endregion

                            #region Окончание матча или еще одни оверы

                            if ((Math.Abs(match.AScoreOvertime - match.BScoreOvertime) >= 2) && (match.AScoreOvertime == match.MaxRounds + 1 || match.BScoreOvertime == match.MaxRounds + 1))
                            {
                                await OnEndMatch(tags, result.WinningTeam, info, server);

                                if (mapQueue.Count > 0 && isBestOfThree)
                                {
                                    await RconHelper.SendMessage(rcon, $"Автоматическая смены карты на {mapQueue.FirstOrDefault().Trim()} через минуту!", Colors.legendary);
                                    Thread.Sleep(60000);
                                    await RconHelper.SendCmd(rcon, $"changelevel {mapQueue.FirstOrDefault().Trim()}");
                                    mapQueue.Remove(mapQueue.FirstOrDefault());
                                }
                                else if (mapQueue.Count == 0)
                                {
                                    isBestOfThree = false;
                                }
                            }

                            if (match.AScoreOvertime + match.BScoreOvertime == match.MaxRounds * 2)
                            {
                                if (match.IsMatch)
                                {
                                    match.AScoreOvertime = 0;
                                    match.BScoreOvertime = 0;
                                    match.IsOvertime = true;
                                    await RconHelper.SendMessage(rcon, "Овертайм!!!", Colors.crimson);
                                    await RconHelper.SendCmd(rcon, "sv_pausable 1");
                                    await RconHelper.SendCmd(rcon, "mp_freezetime 30");
                                    Thread.Sleep(2000);
                                    await RconHelper.SendMessage(rcon, "Полминуты перерыва!", Colors.crimson);
                                    isResetFreezeTime = true;
                                    match.MaxRounds = 3;
                                }
                            }

                            #endregion
                        }

                        #endregion

                        #endregion
                    }
                    if (match.KnifeRound)
                    {
                        int side = 0;
                        if (result.WinningTeam == tName)
                        {
                            side = 2;
                        }
                        else side = 3;
                        await RconHelper.SendCmd(rcon, "exec ktvcss/on_knives_end.cfg");
                        await RconHelper.SendCmd(rcon, $"ChangeVote {side}");
                        match.KnifeRound = false;
                    }
                    await rcon.SendCommandAsync($"score_set {match.BScore} {match.AScore}");
                });

                log.Listen<ChatMessage>(async chat =>
                {
                    await ServerEvents.InsertChatMessage(chat.Player.SteamId, chat.Message, ServerID);
                    int.TryParse(chat.Message, out int mapNum);
                    if (chat.Channel == MessageChannel.All && mapNum > 0 && mapNum < 10 && currentMapSelector == chat.Player.Name)
                    {
                        if (mapPool.Count() == 1)
                        {
                            await RconHelper.SendMessage(rcon, $"Нет карт, которые можно было бы выбрать или запретить!", Colors.legendary);
                            return;
                        }
                        if (!mapPool.ContainsKey(mapNum))
                        {
                            await RconHelper.SendMessage(rcon, $"Вы выбрали карту, которая уже была забанена или выбрана!", Colors.crimson);
                            return;
                        }

                        if (isBestOfOneStarted && mapPool.Count() != 1)
                        {
                            mapPool.Remove(mapNum);
                        }

                        if (isBestOfThree)
                        {
                            if (mapPool.Count() == 6 || mapPool.Count() == 7)
                            {
                                mapQueue.Add(mapPool[mapNum]);
                                mapPool.Remove(mapNum);
                            }
                            else
                            {
                                if (mapPool.Count() != 1)
                                {
                                    mapPool.Remove(mapNum);
                                }  
                            }
                        }

                        if (currentMapSelector == ctPlayerSelector)
                        {
                            currentMapSelector = terPlayerSelector;
                        }
                        else
                        {
                            currentMapSelector = ctPlayerSelector;
                        }

                        string mapList = "Maps:\\n";

                        foreach (var map in mapPool)
                        {
                            mapList += $"{map.Key} - {map.Value.Trim()}\\n";
                        }

                        await RconHelper.SendCmd(rcon, "sm_msay " + mapList);

                        if (mapPool.Count() == 1)
                        {
                            if (isBestOfOneStarted)
                            {
                                await RconHelper.SendMessage(rcon, $"Выбрана карта: {mapPool.FirstOrDefault().Value.Trim()}", Colors.legendary);
                                await RconHelper.SendMessage(rcon, $"Автоматическая смена карты на {mapPool.FirstOrDefault().Value.Trim()} через 5 секунд!", Colors.legendary);
                                Thread.Sleep(5000);
                                await RconHelper.SendCmd(rcon, $"changelevel {mapPool.FirstOrDefault().Value}");
                                currentMapSelector = string.Empty;
                                isBestOfOneStarted = false;
                                terPlayerSelector = string.Empty;
                                ctPlayerSelector = string.Empty;
                                mapPool.Clear();
                            }
                            if (isBestOfThree)
                            {
                                mapQueue.Add(mapPool.FirstOrDefault().Value.Trim());
                                await RconHelper.SendMessage(rcon, $"Решающей картой будет: {mapPool.FirstOrDefault().Value.Trim()}", Colors.legendary);
                                await RconHelper.SendMessage(rcon, $"Автоматическая смена карты на {mapQueue.FirstOrDefault().Trim()} через 5 секунд!", Colors.legendary);
                                Thread.Sleep(5000);
                                await RconHelper.SendCmd(rcon, $"changelevel {mapQueue.FirstOrDefault().Trim()}");
                                mapQueue.Remove(mapQueue.FirstOrDefault());
                                currentMapSelector = string.Empty;
                                isBestOfOneStarted = false;
                                terPlayerSelector = string.Empty;
                                ctPlayerSelector = string.Empty;
                                mapPool.Clear();
                            }
                        }

                        if (isBestOfOneStarted)
                        {
                            if (mapPool.Count() != 1)
                            {
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, Ваша очередь банить карту!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, пожалуйста, напишите номер карты, чтобы забанить ее!", Colors.legendary);
                            }
                        }

                        if (isBestOfThree)
                        {
                            if (mapPool.Count() == 6 || mapPool.Count() == 7)
                            {
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, Ваша очередь пикать карту!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, пожалуйста, напишите номер карты, чтобы пикнуть ее!", Colors.legendary);
                            }
                            else
                            {
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, Ваша очередь банить карту!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, пожалуйста, напишите номер карты, чтобы забанить ее!", Colors.legendary);
                            }
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!me")
                    {
                        var info = await ServerEvents.PrintPlayerInfo(chat.Player.SteamId);

                        if (info.IsCalibration == 0)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {chat.Player.Name} [{info.MMR} MMR]", Colors.ivory);
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%", Colors.ivory);
                        }

                        if (info.IsCalibration == 1)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {chat.Player.Name}", Colors.ivory);
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%", Colors.ivory);
                            await RconHelper.SendMessage(rcon, $"Матчей до конца калибровки: {10 - info.MatchesPlayed}", Colors.ivory);
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!pause" && match.IsMatch)
                    {
                        if (!match.Pause)
                        {
                            if (match.TacticalPauses != 0)
                            {
                                await RconHelper.SendMessage(rcon, "По окончании раунда будет установлена минутная пауза!", Colors.legendary);
                                await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                                match.TacticalPauses--;
                                match.Pause = true;
                                await RconHelper.SendMessage(rcon, $"У Вас осталось {match.TacticalPauses} тактических перерывов!", Colors.legendary);
                            }
                            else
                            {
                                await RconHelper.SendMessage(rcon, "Вы больше не можете брать тактический перерыв!", Colors.crimson);
                            }
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "Нельзя требовать паузу во время паузы!", Colors.crimson);
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!pause5" && match.IsMatch)
                    {
                        if (!match.Pause)
                        {
                            if (match.TechnicalPauses != 0)
                            {
                                await RconHelper.SendMessage(rcon, "По окончании раунда будет установлена пятиминутная пауза!", Colors.legendary);
                                await RconHelper.SendCmd(rcon, "mp_freezetime 300");
                                match.TechnicalPauses--;
                                match.Pause = true;
                                await RconHelper.SendMessage(rcon, $"У Вас осталось {match.TechnicalPauses} технических перерывов!", Colors.legendary);
                            }
                            else
                            {
                                await RconHelper.SendMessage(rcon, "Вы больше не можете брать технический перерыв!", Colors.crimson);
                            }
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "Нельзя требовать паузу во время паузы!", Colors.crimson);
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!bo3") && !isBestOfOneStarted && !isBestOfThree && !match.IsMatch && isCanBeginMatch && !match.KnifeRound)
                    {
                        try
                        {
                            using (SqlConnection connection = new SqlConnection(ConfigTools.Config.SQLConnectionString))
                            {
                                connection.Open();
                                mapPool = new Dictionary<int, string>();
                                SqlCommand query = new SqlCommand($"SELECT MAP FROM [dbo].[MapPool]", connection);
                                using (var reader = query.ExecuteReader())
                                {
                                    int i = 1;
                                    while (reader.Read())
                                    {
                                        mapPool.Add(i, reader[0].ToString());
                                        i++;
                                    }
                                }
                                connection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
                        }

                        var result = await rcon.SendCommandAsync("sm_usrlst");

                        string mapList = "Maps:\\n";

                        foreach (var map in mapPool)
                        {
                            mapList += $"{map.Key} - {map.Value.Trim()}\\n";
                        }

                        var tempPlayers = result.Split('\n');
                        if (tempPlayers.Count() < 3) return;
                        isBestOfThree = true;

                        await RconHelper.SendCmd(rcon, "sm_msay " + mapList);

                        terPlayerSelector = tempPlayers.First(x => x.Contains("\t2")).Split('\t')[0];
                        ctPlayerSelector = tempPlayers.First(x => x.Contains("\t3")).Split('\t')[0];

                        var random = new Random();
                        var rnd = random.Next(0, 2);
                        if (rnd == 0)
                        {
                            currentMapSelector = terPlayerSelector;
                        }
                        else
                        {
                            currentMapSelector = ctPlayerSelector;
                        }

                        await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, Ваша очередь банить карту!");
                        await RconHelper.SendMessage(rcon, $"Первым начинает - {currentMapSelector}", Colors.crimson);
                        await RconHelper.SendMessage(rcon, $"Пожалуйста, напишите номер карты, чтобы ее забанить:", Colors.legendary);
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!bo1") && !isBestOfOneStarted && !isBestOfThree && !match.IsMatch && isCanBeginMatch && !match.KnifeRound)
                    {
                        try
                        {
                            using (SqlConnection connection = new SqlConnection(ConfigTools.Config.SQLConnectionString))
                            {
                                connection.Open();
                                mapPool = new Dictionary<int, string>();
                                SqlCommand query = new SqlCommand($"SELECT MAP FROM [dbo].[MapPool]", connection);
                                using (var reader = query.ExecuteReader())
                                {
                                    int i = 1;
                                    while (reader.Read())
                                    {
                                        mapPool.Add(i, reader[0].ToString());
                                        i++;
                                    }
                                }
                                connection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
                        }

                        var result = await rcon.SendCommandAsync("sm_usrlst");

                        string mapList = "Maps:\\n";

                        foreach (var map in mapPool)
                        {
                            mapList += $"{map.Key} - {map.Value.Trim()}\\n";
                        }

                        var tempPlayers = result.Split('\n');
                        if (tempPlayers.Count() < 3) return;
                        isBestOfOneStarted = true;

                        await RconHelper.SendCmd(rcon, "sm_msay " + mapList);

                        terPlayerSelector = tempPlayers.First(x => x.Contains("\t2")).Split('\t')[0];
                        ctPlayerSelector = tempPlayers.First(x => x.Contains("\t3")).Split('\t')[0];

                        var random = new Random();
                        var rnd = random.Next(0, 2);
                        if (rnd == 0)
                        {
                            currentMapSelector = terPlayerSelector;
                        }
                        else
                        {
                            currentMapSelector = ctPlayerSelector;
                        }

                        Thread boThread = new Thread(CheckIsDeadMapVote)
                        {
                            IsBackground = true
                        };
                        boThread.Start();

                        await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, Ваша очередь банить карту!");
                        await RconHelper.SendMessage(rcon, $"Первым начинает - {currentMapSelector}", Colors.crimson);
                        await RconHelper.SendMessage(rcon, $"Пожалуйста, напишите номер карты, чтобы ее забанить:", Colors.legendary);
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!ko3") && isCanBeginMatch && !match.KnifeRound)
                    {
                        if (OnlinePlayers.Count < match.MinPlayersToStart)
                        {
                            await RconHelper.SendCmd(rcon, "sm_msay Матч не может быть запущен, пока игроков менее восьми");
                            return;
                        }
                        if (!match.IsMatch)
                        {
                            match.KnifeRound = true;
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_knives_start.cfg");
                            await RconHelper.Knives(rcon);
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!lo3") && isCanBeginMatch && !match.KnifeRound)
                    {
                        if (OnlinePlayers.Count < match.MinPlayersToStart)
                        {
                            await RconHelper.SendCmd(rcon, "sm_msay Матч не может быть запущен, пока игроков менее восьми");
                            return;
                        }
                        if (!match.IsMatch)
                        {
                            isCanBeginMatch = false;
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_start.cfg");
                            SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                            currentMapName = info.Map;
                            DemoName = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss") + "_" + info.Map;
                            await RconHelper.SendCmd(rcon, "tv_record " + DemoName);
                            await RconHelper.LiveOnThree(rcon, match, OnlinePlayers);
                            match = new Match(15);
                            match.MatchId = await MatchEvents.CreateMatch(server.ID, info.Map);
                            MatchPlayers = new List<Player>();
                            MatchPlayers.AddRange(OnlinePlayers);
                            foreach (var player in MatchPlayers)
                            {
                                await ServerEvents.AuthPlayer(player.SteamId, player.Name);
                            }
                            foreach (var player in PlayersRank)
                            {
                                await MatchEvents.InsertPlayerRatingProgress(player.SteamID, player.Points);
                            }
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!recover") && isCanBeginMatch)
                    {
                        // not done
                        return;
                        var recoveredMatchID = await MatchEvents.CheckMatchLiveExists(server.ID);
                        if (recoveredMatchID != 0)
                        {
                            //await RconHelper.SendMessage(rcon, "RECOVERED LIVE");
                            MatchPlayers = new List<Player>();
                            MatchPlayers.AddRange(OnlinePlayers);
                            match = new Match(15);
                            match.MatchId = recoveredMatchID;
                            match = await MatchEvents.GetLiveMatchResults(server.ID, match);
                            match.RoundID = match.AScore + match.BScore;
                            if (match.AScore + match.BScore >= match.MaxRounds)
                            {
                                match.FirstHalf = false;
                            }
                            //await RconHelper.SendMessage(rcon, $"{tName} [{match.AScore}-{match.BScore}] {ctName}");
                            foreach (var player in MatchPlayers)
                            {
                                await ServerEvents.AuthPlayer(player.SteamId, player.Name);
                            }
                        }
                        else
                        {
                            //await RconHelper.SendMessage(rcon, "Не найдено матчей для восстановления!");
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!score") && match.IsMatch)
                    {
                        SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                        var tags = MatchEvents.GetTeamNames(MatchPlayers);
                        await RconHelper.SendCmd(rcon, "sys_say {crimson}" + tags[tName] + " {ivory}[" + match.AScore + "-" + match.BScore + "]{dodgerblue} " + tags[ctName]);
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!gethelp") && !match.IsMatch)
                    {
                        await RconHelper.SendMessage(rcon, "!ko3 - запустить ножевой раунд", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!lo3 - запустить матч", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!bo1 - запустить голосование для bo1 матча", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!bo3 - запустить голосование для bo3 матча", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!score - выводит счет матча", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!cm - принудительно остановить матч", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!me - вывод Вашей статистики", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!pause - взять минутный перерыв (4 раза за матч)", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!pause5 - взять пятиминутный перерыв (2 раза за матч)", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!recover - восстанавливает матч после краша сервера (пока отключено)", Colors.ivory);
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!check")
                    {
                        await RconHelper.SendCmd(rcon, "sm_vote \"Вы готовы?\"");
                    }

                    string[] chatWords = chat.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string word in chatWords)
                    {
                        if (ForbiddenWords.Contains(word.ToLower()))
                        {
                            await RconHelper.SendCmd(rcon, $"sm_gag \"{chat.Player.Name}\" 10 Вы были заглушены за использование запрещенных слов ({word})");
                            await RconHelper.SendMessage(rcon, $"{chat.Player.Name} был заглушен за использование запрещенных слов ({word})", Colors.crimson);
                            break;
                        }
                    }
                });

                log.Listen<PlayerConnected>(async connection =>
                {
                    if (connection.Player.SteamId != "STEAM_ID_PENDING" && connection.Player.SteamId != "BOT")
                    {
                        var result = await ServerEvents.AuthPlayer(connection.Player.SteamId, connection.Player.Name);
                        var info = await ServerEvents.PrintPlayerInfo(connection.Player.SteamId);
                        var playerRank = await ServerEvents.GetPlayerRank(connection.Player.SteamId);
                        await ServerEvents.InsertConnectData(ServerID, connection);

                        if (info.IsCalibration == 0)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {connection.Player.Name} [{info.MMR} MMR]", Colors.ivory);
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%", Colors.ivory);
                        }

                        if (info.IsCalibration == 1)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {connection.Player.Name}", Colors.ivory);
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%", Colors.ivory);
                            await RconHelper.SendMessage(rcon, $"Матчей до конца калибровки: {10 - info.MatchesPlayed}", Colors.ivory);
                        }

                        if (OnlinePlayers.Where(x => x.SteamId == connection.Player.SteamId).Count() == 0)
                        {
                            OnlinePlayers.Add(connection.Player);
                            NeedRestart = true;
                            if (match.IsMatch)
                            {
                                if (MatchPlayers.Where(x => x.SteamId == connection.Player.SteamId).Count() == 0) MatchPlayers.Add(connection.Player);
                            }
                        }

                        if (PlayersRank.Where(x => x.SteamID == connection.Player.SteamId).Count() == 0)
                        {
                            PlayersRank.Add(playerRank);
                        }
                        else
                        {
                            PlayersRank.RemoveAll(x => x.SteamID == connection.Player.SteamId);
                            PlayersRank.Add(playerRank);
                        }

                        foreach (string word in ForbiddenWords)
                        {
                            if (connection.Player.Name.ToLower().Contains(word))
                            {
                                await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} Ваш ник содержит запрещенные слова ({word})");
                                break;
                            }
                        }

                        if (!await ServerEvents.IsUserRegistered(connection.Player.SteamId))
                        {
                            await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} Вам нужно привязать VK к группе vk.com/im?sel=-55788587 (команда !setid {connection.Player.SteamId})");
                        }

                        if (await ServerEvents.CheckIsBanned(connection.Player.SteamId))
                        {
                            await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} Вы были заблокированны на проекте. Для уточнения информации посетите Ваш профиль на сайте проекта.");
                        }

                        Logger.Print(server.ID, $"{connection.Player.Name} ({connection.Player.SteamId}) has been connected to {endpoint.Address}:{endpoint.Port}", LogLevel.Trace);
                    }
                });

                log.Listen<ChangeSides>(async result =>
                {
                    await RconHelper.SendCmd(rcon, "sm_swap @all");
                    await RconHelper.SendMessage(rcon, "Напишите !lo3 для запуска матча!", Colors.mediumseagreen);
                });

                log.Listen<MapChange>(async result =>
                {
                    Logger.Print(server.ID, $"Map change to {result.Map}", LogLevel.Trace);

                    if (match.IsMatch)
                    {
                        if (match.AScore == 0 && match.BScore == 0 || (match.AScore + match.BScore < 8))
                        {
                            await RconHelper.SendCmd(rcon, "tv_stoprecord");
                            await MatchEvents.ResetMatch(match.MatchId, server.ID);
                            await RconHelper.SendMessage(rcon, "Матч отменен!", Colors.crimson);
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                            await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                            isCanBeginMatch = true;
                            match = new Match(0);
                        }
                        else
                        {
                            string winner = string.Empty;
                            string looser = string.Empty;
                            if (match.AScore > match.BScore)
                            {
                                winner = tName;
                                looser = ctName;
                            }
                            else
                            {
                                winner = ctName;
                                looser = tName;
                            }

                            var tags = MatchEvents.GetTeamNames(MatchPlayers);
                            await OnEndMatch(tags, winner, currentMapName, server);
                        }
                    }
                });

                log.Listen<NoChangeSides>(async result =>
                {
                    await RconHelper.SendMessage(rcon, "Напишите !lo3 для запуска матча!", Colors.mediumseagreen);
                });

                log.Listen<NameChange>(async result =>
                {
                    foreach (string word in ForbiddenWords)
                    {
                        if (result.NewName.ToLower().Contains(word))
                        {
                            await RconHelper.SendCmd(rcon, $"kickid {result.Player.ClientId} Ваш ник содержит запрещенные слова ({word})");
                            break;
                        }
                    }

                    if (OnlinePlayers.Where(x => x.SteamId == result.Player.SteamId).Count() != 0)
                    {
                        OnlinePlayers.Where(x => x.SteamId == result.Player.SteamId).First().Name = result.NewName;
                    }
                    if (MatchPlayers is not null)
                    {
                        if (MatchPlayers.Where(x => x.SteamId == result.Player.SteamId).Count() != 0)
                        {
                            MatchPlayers.Where(x => x.SteamId == result.Player.SteamId).First().Name = result.NewName;
                        }
                    }
                });

                log.Listen<CancelMatch>(async result =>
                {
                    if (match.IsMatch)
                    {
                        if (match.AScore == 0 && match.BScore == 0 || (match.AScore + match.BScore < 8))
                        {
                            await RconHelper.SendCmd(rcon, "tv_stoprecord");
                            await MatchEvents.ResetMatch(match.MatchId, server.ID);
                            await RconHelper.SendMessage(rcon, "Матч был отменен!", Colors.crimson);
                            Thread.Sleep(3000);
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                            await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                            isCanBeginMatch = true;
                            match = new Match(0);
                            return;
                        }
                        string winner = string.Empty;
                        string looser = string.Empty;
                        if (match.AScore > match.BScore)
                        {
                            winner = tName;
                            looser = ctName;
                        }
                        else
                        {
                            winner = ctName;
                            looser = tName;
                        }

                        SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                        var tags = MatchEvents.GetTeamNames(MatchPlayers);

                        await OnEndMatch(tags, winner, info, server);

                        if (mapQueue.Count > 0 && isBestOfThree)
                        {
                            await RconHelper.SendMessage(rcon, $"Автоматическая смена карты на {mapQueue.FirstOrDefault().Trim()} через минуту!", Colors.legendary);
                            Thread.Sleep(60000);
                            await RconHelper.SendCmd(rcon, $"changelevel {mapQueue.FirstOrDefault().Trim()}");
                            mapQueue.Remove(mapQueue.FirstOrDefault());
                        }
                        else if (mapQueue.Count == 0)
                        {
                            isBestOfThree = false;
                        }
                    }
                    if (match.KnifeRound)
                    {
                        await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                        await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                        match.KnifeRound = !match.KnifeRound;
                    }
                });

                log.Listen<PlayerDisconnected>(async connection =>
                {
                    if (connection.Player.SteamId != "BOT")
                    {
                        OnlinePlayers.RemoveAll(x => x.SteamId == connection.Player.SteamId);
                        if (!match.IsMatch)
                        {
                            PlayersRank.RemoveAll(x => x.SteamID == connection.Player.SteamId);
                        }
                        await ServerEvents.InsertDisconnectData(ServerID, connection);
                        Logger.Print(server.ID, $"{connection.Player.Name} ({connection.Player.SteamId}) has been disconnected from {endpoint.Address}:{endpoint.Port} ({connection.Reason})", LogLevel.Trace);
                        if (OnlinePlayers.Count() == 0 && NeedRestart)
                        {
                            Logger.Print(server.ID, "Autorestart cuz players count is zero", LogLevel.Debug);
                            Environment.Exit(0);
                        }
                    }
                });

                log.Listen<MatchBackup>(async data =>
                {
                    if (match.IsMatch)
                    {
                        if (!match.Backups.Where(x => x.SteamID == data.SteamID).Any())
                        {
                            match.Backups.Add(data);
                        }
                        else
                        {
                            match.Backups.RemoveAll(x => x.SteamID == data.SteamID);
                            match.Backups.Add(data);
                        }
                        await MatchEvents.InsertMatchBackupRecord(match, data);
                    }
                });

                await Task.Delay(-1);
            }

            private async Task OnEndMatch(Dictionary<string, string> tags, string winningTeam, SourceQueryInfo info, Server server)
            {
                string looser = string.Empty;

                if (winningTeam == tName)
                {
                    looser = ctName;
                }
                else
                {
                    looser = tName;
                }

                await RconHelper.SendCmd(rcon, $"sm_msay {tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\nМатч завершен!\\nПоздравляем с победой команду {tags[winningTeam]}!\\n{tags[looser]}, в следующий раз Вам повезет.");
                await RconHelper.SendMessage(rcon, "Матч завершен!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"Поздравляем с победой команду {tags[winningTeam]}!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"{tags[looser]}, в следующий раз Вам повезет.", Colors.mediumseagreen);
                await MatchEvents.InsertMatchLog(match.MatchId, $"<Match End>", info.Map, server.ID);
                await MatchEvents.InsertDemoName(match.MatchId, DemoName);
                MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], info.Map, server.ID, MatchPlayers, winningTeam, match);
                
                isCanBeginMatch = true;
                Match bMatch = match;

                MatchResultInfo matchResultInfo = new MatchResultInfo
                {
                    MapName = info.Map,
                    MatchScore = VKInteraction.Matches.GetMatchResult(bMatch.MatchId).Result,
                    MVPlayer = VKInteraction.Matches.GetMatchMVP(bMatch.MatchId).Result,
                    PlayerResults = VKInteraction.Matches.GetPlayerResults(bMatch.MatchId).Result
                };

                if (!matchResultInfo.MatchScore.AName.Contains("Team ") && !matchResultInfo.MatchScore.BName.Contains("Team "))
                {
                    VKInteraction.Matches.PublishResult(matchResultInfo);
                }
                else 
                {
                    Program.Node.FTPTools.DownloadFile(Program.Node.DemoName + ".dem");
                    Program.Node.FTPTools.UploadFile(Program.Node.DemoName + ".dem.zip");
                }

                match = new Match(0);
                Thread.Sleep(5000);

                await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                await RconHelper.SendCmd(rcon, "tv_stoprecord");
            }

            private async Task OnEndMatch(Dictionary<string, string> tags, string winningTeam, string mapName, Server server)
            {
                if (!match.IsMatch) return;

                string looser = string.Empty;

                if (winningTeam == tName)
                {
                    looser = ctName;
                }
                else
                {
                    looser = tName;
                }

                await RconHelper.SendCmd(rcon, $"sm_msay {tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\nМатч завершен!\\nПоздравляем с победой команду {tags[winningTeam]}!\\n{tags[looser]}, в следующий раз Вам повезет.");
                await RconHelper.SendMessage(rcon, "Матч завершен!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"Поздравляем с победой команду {tags[winningTeam]}!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"{tags[looser]}, в следующий раз Вам повезет.", Colors.mediumseagreen);
                await MatchEvents.InsertMatchLog(match.MatchId, $"<Match End>", mapName, server.ID);
                await MatchEvents.InsertDemoName(match.MatchId, DemoName);
                MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], mapName, server.ID, MatchPlayers, winningTeam, match);
                
                isCanBeginMatch = true;
                Match bMatch = match;

                MatchResultInfo matchResultInfo = new MatchResultInfo
                {
                    MapName = mapName,
                    MatchScore = VKInteraction.Matches.GetMatchResult(bMatch.MatchId).Result,
                    MVPlayer = VKInteraction.Matches.GetMatchMVP(bMatch.MatchId).Result,
                    PlayerResults = VKInteraction.Matches.GetPlayerResults(bMatch.MatchId).Result
                };

                if (!matchResultInfo.MatchScore.AName.Contains("Team ") && !matchResultInfo.MatchScore.BName.Contains("Team "))
                {
                    VKInteraction.Matches.PublishResult(matchResultInfo);
                }
                else 
                {
                    Program.Node.FTPTools.DownloadFile(Program.Node.DemoName + ".dem");
                    Program.Node.FTPTools.UploadFile(Program.Node.DemoName + ".dem.zip");
                }

                match = new Match(0);
                Thread.Sleep(5000);

                await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                await RconHelper.SendCmd(rcon, "tv_stoprecord");
            }

            private void Rcon_OnDisconnected()
            {
                Logger.Print(ServerID, "RCON connection is closed", LogLevel.Warn);
                if (!match.IsMatch)
                {
                    isCanBeginMatch = true;
                }
            }

            private async void PrintAlertMessages(RCON rcon)
            {
                await RconHelper.SendMessage(rcon, "Базовые команды: !ko3 !lo3 !bo1 !bo3 !me !cm !pause !pause5 !check", Colors.ivory);
                await RconHelper.SendMessage(rcon, "Напишите !gethelp для получения описания всех команд", Colors.ivory);
                await RconHelper.SendMessage(rcon, "Примечание: запуск матча может быть осуществлен без админских прав", Colors.crimson);
//#if DEBUG
                await RconHelper.SendMessage(rcon, "The process has been started in debug mode", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, "If you spot a bug please submit it to developer", Colors.mediumseagreen);
//#endif
            }

            private async void CheckIsDeadMapVote()
            {
                Thread.Sleep(120 * 1000);
                if (isBestOfOneStarted)
                {
                    isBestOfOneStarted = !isBestOfOneStarted;
                    await RconHelper.SendMessage(rcon, "Голосование bo1/bo3 было аннулировано!", Colors.crimson);
                }
            }

            private static void SetAutoRestartTimer()
            {
                aTimer = new System.Timers.Timer(15 * 60 * 1000);
                aTimer.Elapsed += ATimer_Elapsed;
                aTimer.AutoReset = true;
                aTimer.Enabled = true;
            }

            private static void ATimer_Elapsed(object sender, ElapsedEventArgs e)
            {
                if (OnlinePlayers.Count() == 0)
                {
                    Logger.Print(ServerID, "Autorestart (on timer) cuz players count is zero", LogLevel.Debug);
                    Environment.Exit(0);
                }
            }

            private async void Alerter(object _server)
            {
                Server server = (Server)_server;
                Thread.Sleep(30000);
                while (true)
                {
                    if (!match.IsMatch)
                    {
                        PrintAlertMessages(rcon);
                    }
                    else
                    {
                        if (OnlinePlayers.Count < match.MinPlayersToStop)
                        {
                            if (match.AScore == 0 && match.BScore == 0 || (match.AScore + match.BScore < 8))
                            {
                                await RconHelper.SendCmd(rcon, "tv_stoprecord");
                                await MatchEvents.ResetMatch(match.MatchId, server.ID);
                                await RconHelper.SendMessage(rcon, "Матч отменен!", Colors.crimson);
                                await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                                await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                                isCanBeginMatch = true;
                                match = new Match(0);
                            }
                            else
                            {
                                string winner = string.Empty;
                                string looser = string.Empty;
                                if (match.AScore > match.BScore)
                                {
                                    winner = tName;
                                    looser = ctName;
                                }
                                else
                                {
                                    winner = ctName;
                                    looser = tName;
                                }

                                var tags = MatchEvents.GetTeamNames(MatchPlayers);
                                await OnEndMatch(tags, winner, currentMapName, server);
                            }

                            if (mapQueue.Count > 0 && isBestOfThree)
                            {
                                await RconHelper.SendMessage(rcon, $"Автоматическая смена карты на {mapQueue.FirstOrDefault().Trim()} через минуту!", Colors.legendary);
                                Thread.Sleep(60000);
                                await RconHelper.SendCmd(rcon, $"changelevel {mapQueue.FirstOrDefault().Trim()}");
                                mapQueue.Remove(mapQueue.FirstOrDefault());
                            }
                            else if (mapQueue.Count == 0)
                            {
                                isBestOfThree = false;
                            }
                        }
                    }
                    Thread.Sleep(60000);
                }
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "kTVCSS NODE LAUNCHER";
            Console.ForegroundColor = ConsoleColor.Green;
            Logger.Print(0, "Welcome, " + Environment.UserName, LogLevel.Info);
            Logger.Print(0, $"STATGROUP - {ConfigTools.Config.StatGroupID}", LogLevel.Debug);
            Logger.Print(0, $"ADMINID - {ConfigTools.Config.AdminVkID}", LogLevel.Debug);
            Logger.Print(0, $"SQLCONSTR - {ConfigTools.Config.SQLConnectionString}", LogLevel.Debug);
            Logger.Print(0, $"VKTOKEN - {ConfigTools.Config.VKToken}", LogLevel.Debug);

#if DEBUG

            args = new string[1];
            args[0] = "0";

#endif

            if (args.Length != 0)
            {
                Loader.LoadServers();

                ForbiddenWords.AddRange(File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "wordsfilter.txt"), System.Text.Encoding.UTF8));
                Logger.Print(0, "Words filter has been loaded", LogLevel.Info);

                Console.Title = "kTVCSS @ " + Servers[int.Parse(args[0])].Host + ":" + Servers[int.Parse(args[0])].GamePort;

                Node node = new Node();
                Task.Run(async () => await node.StartNode(Servers[int.Parse(args[0])])).GetAwaiter().GetResult();
            }
        }
    }
}
