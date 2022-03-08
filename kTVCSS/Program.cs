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

            public List<Player> MatchPlayers = null;
            public static List<Player> OnlinePlayers = new List<Player>();
            public static int ServerID = 0;

            private bool isCanBeginMatch = true;
            private bool isResetFreezeTime = false;
            private bool isBestOfOneStarted = false;
            private bool isBestOfThree = false;
            private string currentMapSelector = string.Empty;
            private string terPlayerSelector = string.Empty;
            private string ctPlayerSelector = string.Empty;
            private string tName = "TERRORIST";
            private string ctName = "CT";
            private string demoName = string.Empty;
            private string currentMapName = string.Empty;
            
            public async Task StartNode(Server server)
            {
                Logger.LoggerID = server.ID;

                ServerID = server.ID;
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
                Logger.Print(server.ID, $"Created connection to {info.Name}", LogLevel.Trace);
                await RconHelper.SendMessage(rcon, "Соединение до центрального сервера kTVCSS установлено!");
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
                            await OnPlayerConnectAuth.AuthPlayer(kill.Killer.SteamId, kill.Killer.Name);
                        }
                        else
                        {
                            match.PlayerKills[kill.Killer.SteamId]++;
                        }

                        await OnPlayerKill.SetValues(kill.Killer.Name, kill.Killed.Name, kill.Killer.SteamId, kill.Killed.SteamId, hs, server.ID, match.MatchId);

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
                            //await RconHelper.SendMessage(rcon, "Выход с матча приведет к блокировке");
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
                                            //await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a triple kill!");
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a triple kill!", info.Map, server.ID);
                                            break;
                                        }
                                    case 4:
                                        {
                                            //await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a quad kill!");
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a quad kill!", info.Map, server.ID);
                                            break;
                                        }
                                    case 5:
                                        {
                                            //await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!");
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!", info.Map, server.ID);
                                            break;
                                        }
                                }
                            }
                        }
                        await MatchEvents.SetOpenFrag(match.OpenFragSteamID);
                        await MatchEvents.UpdateMatchScore(match.AScore, match.BScore, server.ID, match.MatchId);

                        await RconHelper.SendMessage(rcon, $"{tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}");
                        if (match.AScore + match.BScore == match.MaxRounds || (match.AScoreOvertime + match.BScoreOvertime == match.MaxRounds && match.AScoreOvertime != match.BScoreOvertime))
                        {
                            await RconHelper.SendCmd(rcon, "sm_msay " + $"{tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\nСейчас будет установлен тайм-аут на одну минуту.\\nМатч продолжится автоматически.");
                            await RconHelper.SendMessage(rcon, "Тайм-аут!");
                            await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                            Thread.Sleep(2000);
                            await RconHelper.SendCmd(rcon, "sm_swap @all");
                            await RconHelper.SendCmd(rcon, "sv_pausable 1");
                            await RconHelper.SendMessage(rcon, "Одна минута перерыва!");
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

                        if (match.AScore + match.BScore == match.MaxRounds * 2 || (match.AScoreOvertime + match.BScoreOvertime >= match.MaxRounds + 1 && match.IsOvertime))
                        {
                            if ((Math.Abs(match.AScoreOvertime - match.BScoreOvertime) >= 2) && (match.AScoreOvertime == match.MaxRounds + 1 || match.BScoreOvertime == match.MaxRounds + 1))
                            {
                                await OnEndMatch(tags, result.WinningTeam, info, server);

                                if (mapQueue.Count > 0 && isBestOfThree)
                                {
                                    await RconHelper.SendMessage(rcon, $"Автоматическая смены карты на {mapQueue.FirstOrDefault().Trim()} через минуту!");
                                    Thread.Sleep(60000);
                                    await RconHelper.SendCmd(rcon, $"changelevel {mapQueue.FirstOrDefault().Trim()}");
                                    mapQueue.Remove(mapQueue.FirstOrDefault());
                                }
                                else if (mapQueue.Count == 0)
                                {
                                    isBestOfThree = false;
                                }
                            }
                            else
                            {
                                if (match.AScore + match.BScore == match.MaxRounds * 2 || match.AScoreOvertime + match.BScoreOvertime == match.MaxRounds * 2)
                                {
                                    if (!match.FirstHalf && !match.IsOvertime)
                                    {
                                        if ((Math.Abs(match.AScore - match.BScore) >= 2) && (match.AScore == match.MaxRounds + 1 || match.BScore == match.MaxRounds + 1))
                                        {
                                            await OnEndMatch(tags, result.WinningTeam, info, server);

                                            if (mapQueue.Count > 0 && isBestOfThree)
                                            {
                                                await RconHelper.SendMessage(rcon, $"Автоматическая смены карты на {mapQueue.FirstOrDefault().Trim()} через минуту!"); 
                                                Thread.Sleep(60000);
                                                await RconHelper.SendCmd(rcon, $"changelevel {mapQueue.FirstOrDefault().Trim()}");
                                                mapQueue.Remove(mapQueue.FirstOrDefault());
                                            }
                                            else if (mapQueue.Count == 0)
                                            {
                                                isBestOfThree = false;
                                            }
                                        }
                                        else
                                        {
                                            match.AScoreOvertime = 0;
                                            match.BScoreOvertime = 0;
                                            match.IsOvertime = true;
                                            await RconHelper.SendMessage(rcon, "Овертайм!!!");
                                            await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                                            Thread.Sleep(2000);
                                            await RconHelper.SendMessage(rcon, "Одна минута перерыва!");
                                            isResetFreezeTime = true;
                                            match.MaxRounds = 3;
                                        }
                                    }   
                                }
                            }
                        }

                        if (!match.FirstHalf && !match.IsOvertime && match.IsMatch)
                        {
                            if ((Math.Abs(match.AScore - match.BScore) >= 2) && (match.AScore == match.MaxRounds + 1 || match.BScore == match.MaxRounds + 1))
                            {
                                await OnEndMatch(tags, result.WinningTeam, info, server);

                                if (mapQueue.Count > 0 && isBestOfThree)
                                {
                                    await RconHelper.SendMessage(rcon, $"Автоматическая смены карты на {mapQueue.FirstOrDefault().Trim()} через минуту!");
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
                    int.TryParse(chat.Message, out int mapNum);
                    if (chat.Channel == MessageChannel.All && mapNum > 0 && mapNum < 10 && currentMapSelector == chat.Player.Name)
                    {
                        if (mapPool.Count() == 1)
                        {
                            await RconHelper.SendMessage(rcon, $"Нет карт, которые можно было бы выбрать или запретить!");
                            return;
                        }
                        if (!mapPool.ContainsKey(mapNum))
                        {
                            await RconHelper.SendMessage(rcon, $"Вы выбрали карту, которая уже была забанена или выбрана!");
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
                                await RconHelper.SendMessage(rcon, $"Выбрана карта: {mapPool.FirstOrDefault().Value.Trim()}");
                                await RconHelper.SendMessage(rcon, $"Автоматическая смена карты на {mapPool.FirstOrDefault().Value.Trim()} через 5 секунд!");
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
                                await RconHelper.SendMessage(rcon, $"Решающей картой будет: {mapPool.FirstOrDefault().Value.Trim()}");
                                await RconHelper.SendMessage(rcon, $"Автоматическая смена карты на {mapQueue.FirstOrDefault().Trim()} через 5 секунд!");
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
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, пожалуйста, напишите номер карты, чтобы забанить ее!");
                            }
                        }

                        if (isBestOfThree)
                        {
                            if (mapPool.Count() == 6 || mapPool.Count() == 7)
                            {
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, Ваша очередь пикать карту!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, пожалуйста, напишите номер карты, чтобы пикнуть ее!");
                            }
                            else
                            {
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, Ваша очередь банить карту!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, пожалуйста, напишите номер карты, чтобы забанить ее!");
                            }
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!me")
                    {
                        var info = await OnPlayerJoinTheServer.PrintPlayerInfo(chat.Player.SteamId);

                        if (info.IsCalibration == 0)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {chat.Player.Name} [{info.MMR} MMR]");
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%");
                        }

                        if (info.IsCalibration == 1)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {chat.Player.Name}");
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%");
                            await RconHelper.SendMessage(rcon, $"Matches until the end of calibration: {10 - info.MatchesPlayed}");
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!pause" && match.IsMatch)
                    {
                        if (match.TacticalPauses != 0)
                        {
                            await RconHelper.SendMessage(rcon, "По окончании раунда будет установлена минутная пауза!");
                            await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                            match.TacticalPauses--;
                            match.Pause = true;
                            await RconHelper.SendMessage(rcon, $"У Вас осталось {match.TacticalPauses} тактических перерывов!");
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "Вы больше не можете брать тактический перерыв!");
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!pause5" && match.IsMatch)
                    {
                        if (match.TechnicalPauses != 0)
                        {
                            await RconHelper.SendMessage(rcon, "По окончании раунда будет установлена пятиминутная пауза!");
                            await RconHelper.SendCmd(rcon, "mp_freezetime 300");
                            match.TechnicalPauses--;
                            match.Pause = true;
                            await RconHelper.SendMessage(rcon, $"У Вас осталось {match.TechnicalPauses} технических перерывов!");
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "Вы больше не можете брать технический перерыв!");
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
                        await RconHelper.SendMessage(rcon, $"Первым начинает - {currentMapSelector}");
                        await RconHelper.SendMessage(rcon, $"Пожалуйста, напишите номер карты, чтобы ее забанить:");
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
                        await RconHelper.SendMessage(rcon, $"Первым начинает - {currentMapSelector}");
                        await RconHelper.SendMessage(rcon, $"Пожалуйста, напишите номер карты, чтобы ее забанить:");
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
                        else
                        {
                            if (match.IsMatch)
                            {
                                await RconHelper.SendMessage(rcon, "Матч уже запущен!");
                            }
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
                            demoName = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss") + "_" + info.Map;
                            await RconHelper.SendCmd(rcon, "tv_record " + demoName);
                            await RconHelper.LiveOnThree(rcon, match, OnlinePlayers);
                            match = new Match(15);
                            match.MatchId = await MatchEvents.CreateMatch(server.ID, info.Map);
                            MatchPlayers = new List<Player>();
                            MatchPlayers.AddRange(OnlinePlayers);
                            foreach (var player in MatchPlayers)
                            {
                                await OnPlayerConnectAuth.AuthPlayer(player.SteamId, player.Name);
                            }
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "Матч уже запущен!");
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!recover") && isCanBeginMatch)
                    {
                        // not done
                        var recoveredMatchID = await MatchEvents.CheckMatchLiveExists(server.ID);
                        if (recoveredMatchID != 0)
                        {
                            await RconHelper.SendMessage(rcon, "RECOVERED LIVE");
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
                            await RconHelper.SendMessage(rcon, $"{tName} [{match.AScore}-{match.BScore}] {ctName}");
                            foreach (var player in MatchPlayers)
                            {
                                await OnPlayerConnectAuth.AuthPlayer(player.SteamId, player.Name);
                            }
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "Не найдено матчей для восстановления!");
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!score") && match.IsMatch)
                    {
                        SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                        var tags = MatchEvents.GetTeamNames(MatchPlayers);
                        await RconHelper.SendMessage(rcon, $"{tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}");
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!gethelp") && !match.IsMatch)
                    {
                        await RconHelper.SendMessage(rcon, "!ko3 - запустить ножевой раунд");
                        await RconHelper.SendMessage(rcon, "!lo3 - запустить матч");
                        await RconHelper.SendMessage(rcon, "!bo1 - запустить голосование для bo1 матча");
                        await RconHelper.SendMessage(rcon, "!bo3 - запустить голосование для bo3 матча");
                        await RconHelper.SendMessage(rcon, "!score - выводит счет матча");
                        await RconHelper.SendMessage(rcon, "!cm - принудительно остановить матч");
                        await RconHelper.SendMessage(rcon, "!me - вывод Вашей статистики");
                        await RconHelper.SendMessage(rcon, "!pause - взять минутный перерыв (4 раза за матч)");
                        await RconHelper.SendMessage(rcon, "!pause5 - взять пятиминутный перерыв (2 раза за матч)");
                        await RconHelper.SendMessage(rcon, "!recover - восстанавливает матч после краша сервера (пока отключено)");
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
                            break;
                        }
                    }
                });

                log.Listen<PlayerConnected>(async connection =>
                {
                    if (connection.Player.SteamId != "STEAM_ID_PENDING" && connection.Player.SteamId != "BOT")
                    {
                        var result = await OnPlayerConnectAuth.AuthPlayer(connection.Player.SteamId, connection.Player.Name);
                        var info = await OnPlayerJoinTheServer.PrintPlayerInfo(connection.Player.SteamId);

                        if (info.IsCalibration == 0)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {connection.Player.Name} [{info.MMR} MMR]");
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%");
                        }

                        if (info.IsCalibration == 1)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {connection.Player.Name}");
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%");
                            await RconHelper.SendMessage(rcon, $"Matches until the end of calibration: {10 - info.MatchesPlayed}");
                        }

                        if (OnlinePlayers.Where(x => x.SteamId == connection.Player.SteamId).Count() == 0)
                        {
                            OnlinePlayers.Add(connection.Player);
                            if (match.IsMatch)
                            {
                                if (MatchPlayers.Where(x => x.SteamId == connection.Player.SteamId).Count() == 0) MatchPlayers.Add(connection.Player);
                            }
                        }

                        foreach (string word in ForbiddenWords)
                        {
                            if (connection.Player.Name.ToLower().Contains(word))
                            {
                                await RconHelper.SendCmd(rcon, $"sm_kick #{connection.Player.ClientId} Ваш ник содержит запрещенные слова ({word})");
                                break;
                            }
                        }

                        Logger.Print(server.ID, $"{connection.Player.Name} ({connection.Player.SteamId}) has been connected to {endpoint.Address}:{endpoint.Port}", LogLevel.Trace);
                    }
                });

                log.Listen<ChangeSides>(async result =>
                {
                    await RconHelper.SendCmd(rcon, "sm_swap @all");
                    await RconHelper.SendMessage(rcon, "Напишите !lo3 для запуска матча!");
                });

                log.Listen<NoChangeSides>(async result =>
                {
                    await RconHelper.SendMessage(rcon, "Напишите !lo3 для запуска матча!");
                });

                log.Listen<NameChange>(async result =>
                {
                    foreach (string word in ForbiddenWords)
                    {
                        if (result.NewName.ToLower().Contains(word))
                        {
                            await RconHelper.SendCmd(rcon, $"sm_kick #{result.Player.ClientId} Ваш ник содержит запрещенные слова ({word})");
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

                //log.Listen<NotLive>(async result =>
                //{
                //    await RconHelper.SendMessage(rcon, "!nl is temporarily disabled");
                //    return;
                //    if (!match.IsMatch) return;

                //    if (match.AScore + match.BScore == match.MaxRounds || (match.AScore == 0 && match.BScore == 0))
                //    {
                //        match.IsMatch = false;
                //        await RconHelper.SendMessage(rcon, "The current match half is reseted!");
                //        await RconHelper.SendCmd(rcon, "mp_restartgame 1");
                //    }
                //    else
                //    {
                //        await RconHelper.SendMessage(rcon, "You're not allowed to reset match half!");
                //    }
                //});

                log.Listen<CancelMatch>(async result =>
                {
                    if (match.IsMatch)
                    {
                        if (match.AScore == 0 && match.BScore == 0 || (match.AScore + match.BScore < 8))
                        {
                            await RconHelper.SendCmd(rcon, "tv_stoprecord");
                            await MatchEvents.ResetMatch(match.MatchId, server.ID);
                            await RconHelper.SendMessage(rcon, "Матч был отменен!");
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
                            await RconHelper.SendMessage(rcon, $"Автоматическая смена карты на {mapQueue.FirstOrDefault().Trim()} через минуту!");
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
                        //if (match.IsMatch)
                        //{
                        //    Thread banThread = new Thread(BanOnLeftTheMatch)
                        //    {
                        //        IsBackground = true
                        //    };
                        //    banThread.Start(connection.Player);
                        //}
                        //else
                        //{
                        //    OnlinePlayers.RemoveAll(x => x.SteamId == connection.Player.SteamId);
                        //}
                        Logger.Print(server.ID, $"{connection.Player.Name} ({connection.Player.SteamId}) has been disconnected from {endpoint.Address}:{endpoint.Port} ({connection.Reason})", LogLevel.Trace);
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

            private void InsertBan(string steamid)
            {
                try
                {
                    using (MySqlConnection connection = new MySqlConnection(ConfigTools.Config.SourceBansConnectionString))
                    {
                        connection.Open();
                        string cmd = $"INSERT INTO `sourcebans`.`sb_bans` (`authid`, `created`, `ends`, `length` ,`reason`) VALUES ('{steamid}', UNIX_TIMESTAMP(SYSDATE()), UNIX_TIMESTAMP(SYSDATE()) + 43200, 43200, 'Left the match');";
                        var query = new MySqlCommand(cmd, connection);
                        query.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
                }
            }

            private async void BanOnLeftTheMatch(object _player)
            {
                Player player = (Player)_player;
                await RconHelper.SendMessage(rcon, $"У игрока {player.Name} есть 3 минуты, чтобы не получить бан за выход с матча!");
                int previousPlayersCount = OnlinePlayers.Count;
                OnlinePlayers.RemoveAll(x => x.SteamId == player.SteamId);
                Thread.Sleep(3 * 60 * 1000);
                if (!OnlinePlayers.Where(x => x.SteamId == player.SteamId).Any())
                {
                    if (match.IsMatch && !(previousPlayersCount < OnlinePlayers.Count))
                    {
                        await RconHelper.SendMessage(rcon, $"{player.Name} был заблокирован за выход с матча!");
                        InsertBan(player.SteamId);
                    }
                }
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
                await RconHelper.SendMessage(rcon, "Матч завершен!");
                await RconHelper.SendMessage(rcon, $"Поздравляем с победой команду {tags[winningTeam]}!");
                await RconHelper.SendMessage(rcon, $"{tags[looser]}, в следующий раз Вам повезет.");
                await MatchEvents.InsertMatchLog(match.MatchId, $"<Match End>", info.Map, server.ID);
                await MatchEvents.InsertDemoName(match.MatchId, demoName);
                MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], info.Map, server.ID, MatchPlayers, winningTeam, match);
                
                isCanBeginMatch = true;
                Match bMatch = match;

                // Добавить условие, если микс, то не публикуем

                MatchResultInfo matchResultInfo = new MatchResultInfo
                {
                    MapName = info.Map,
                    MatchScore = VKInteraction.Matches.GetMatchResult(bMatch.MatchId).Result,
                    MVPlayer = VKInteraction.Matches.GetMatchMVP(bMatch.MatchId).Result,
                    PlayerResults = VKInteraction.Matches.GetPlayerResults(bMatch.MatchId).Result
                };

                VKInteraction.Matches.PublishResult(matchResultInfo);

                match = new Match(0);
                Thread.Sleep(3000);
                
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
                await RconHelper.SendMessage(rcon, "Матч завершен!");
                await RconHelper.SendMessage(rcon, $"Поздравляем с победой команду {tags[winningTeam]}!");
                await RconHelper.SendMessage(rcon, $"{tags[looser]}, в следующий раз Вам повезет.");
                await MatchEvents.InsertMatchLog(match.MatchId, $"<Match End>", mapName, server.ID);
                await MatchEvents.InsertDemoName(match.MatchId, demoName);
                MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], mapName, server.ID, MatchPlayers, winningTeam, match);
                
                isCanBeginMatch = true;
                Match bMatch = match;

                // Добавить условие, если микс, то не публикуем

                MatchResultInfo matchResultInfo = new MatchResultInfo
                {
                    MapName = mapName,
                    MatchScore = VKInteraction.Matches.GetMatchResult(bMatch.MatchId).Result,
                    MVPlayer = VKInteraction.Matches.GetMatchMVP(bMatch.MatchId).Result,
                    PlayerResults = VKInteraction.Matches.GetPlayerResults(bMatch.MatchId).Result
                };

                VKInteraction.Matches.PublishResult(matchResultInfo);

                match = new Match(0);
                Thread.Sleep(3000);

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
                await RconHelper.SendMessage(rcon, "Базовые команды: !ko3 !lo3 !bo1 !bo3 !me !cm !pause !pause5 !check");
                await RconHelper.SendMessage(rcon, "Напишите !gethelp для получения описания всех команд");
                await RconHelper.SendMessage(rcon, "Примечание: запуск матча может быть осуществлен без админских прав");
            }

            private async void CheckIsDeadMapVote()
            {
                Thread.Sleep(120 * 1000);
                if (isBestOfOneStarted)
                {
                    isBestOfOneStarted = !isBestOfOneStarted;
                    await RconHelper.SendMessage(rcon, "Голосование bo1/bo3 было аннулировано!");
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
                                await RconHelper.SendMessage(rcon, "Матч отменен!");
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
                                await RconHelper.SendMessage(rcon, $"Автоматическая смена карты на {mapQueue.FirstOrDefault().Trim()} через минуту!");
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

            #if DEBUG

            args = new string[1];
            args[0] = "3";

            #endif

            if (args.Length != 0)
            {
                Loader.LoadServers();

                ForbiddenWords.AddRange(File.ReadAllLines("wordsfilter.txt", System.Text.Encoding.UTF8));
                Logger.Print(0, "Words filter has been loaded", LogLevel.Info);

                Console.Title = "kTVCSS @ " + Servers[int.Parse(args[0])].Host + ":" + Servers[int.Parse(args[0])].Port;

                Node node = new Node();
                Task.Run(async () => await node.StartNode(Servers[int.Parse(args[0])])).GetAwaiter().GetResult();
            }
            //else
            //{
            //    Logger.Print(0, "Attempt to load servers from database", LogLevel.Info);
            //    Loader.LoadServers();
            //    Logger.Print(0, "Loaded " + Servers.Count + " servers", LogLevel.Info);
            //    foreach (var server in Servers)
            //    {
            //        Process node = new Process();
            //        node.StartInfo.UseShellExecute = true;
            //        node.StartInfo.FileName = "kTVCSS.exe";
            //        node.StartInfo.Arguments = (server.ID - 1).ToString();
            //        node.Start();
            //    }
            //}
        }
    }
}
