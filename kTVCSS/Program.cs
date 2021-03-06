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
                Thread.Sleep(1000);
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
                        Logger.Print(server.ID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
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
                await RconHelper.SendMessage(rcon, "???????????????????? ???? ???????????????????????? ?????????????? kTVCSS ??????????????????????!", Colors.ivory);
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

                log.Listen<TeamChange>(async data =>
                {
                    if (!match.IsMatch)
                    {
                        try
                        {
                            if (data.Player.SteamId != "BOT")
                            {
                                if (data.Team != "Spectator")
                                {
                                    OnlinePlayers.Where(x => x.SteamId == data.Player.SteamId).First().Team = data.Team;
                                    Logger.Print(ServerID, $"{data.Player.Name} joined team {data.Team}", LogLevel.Debug);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                        }
                    }
                    else
                    {
                        try
                        {
                            if (data.Player.SteamId != "BOT")
                            {
                                if (data.Team != "Spectator")
                                {
                                    MatchPlayers.Where(x => x.SteamId == data.Player.SteamId).First().Team = data.Team;
                                    Logger.Print(ServerID, $"{data.Player.Name} joined team {data.Team}", LogLevel.Debug);
                                } 
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                        }
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
                            if (match.MatchType == 0)
                            {
                                await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MATCH}");
                            }
                            else
                            {
                                await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MIX}");
                            }
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
                                    try
                                    {
                                        int pts = PlayersRank.Where(x => x.SteamID == player.SteamId).First().Points;
                                        if (pts != 0)
                                        {
                                            terAvg += pts;
                                            terCount++;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // Ignored
                                    }
                                }

                                foreach (var player in cts)
                                {
                                    try
                                    {
                                        int pts = PlayersRank.Where(x => x.SteamID == player.SteamId).First().Points;
                                        if (pts != 0)
                                        {
                                            ctAvg += pts;
                                            ctCount++;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // Ignored
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
                                                if (tags[tName].Contains("Team ") || tags[ctName].Contains("Team "))
                                                {
                                                    match.MatchType = 1;
                                                    await RconHelper.SendMessage(rcon, $"?????????????????? ???????????? ?????? ????????-??????????:", Colors.crimson);
                                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MIX}");
                                                    await RconHelper.SendCmd(rcon, $"mp_friendlyfire {Game.Cvars.FRIENDLYFIRE_MIX}");
                                                }
                                                else
                                                {
                                                    match.MatchType = 0;
                                                    await RconHelper.SendMessage(rcon, $"?????????????????? ???????????? ?????? ???????????????? ??????????:", Colors.crimson);
                                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MATCH}");
                                                    await RconHelper.SendCmd(rcon, $"mp_friendlyfire {Game.Cvars.FRIENDLYFIRE_MATCH}");
                                                }
                                                await RconHelper.SendMessage(rcon, $"?????????????? ?????????????? ?????????????? {tags[tName]} - {ter}", Colors.crimson);
                                                await RconHelper.SendMessage(rcon, $"?????????????? ?????????????? ?????????????? {tags[ctName]} - {ct}", Colors.dodgerblue);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                match.MatchType = 0;
                                await RconHelper.SendMessage(rcon, $"?????????????????? ???????????? ?????? ???????????????? ??????????:", Colors.crimson);
                                await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MATCH}");
                                await RconHelper.SendCmd(rcon, $"mp_friendlyfire {Game.Cvars.FRIENDLYFIRE_MATCH}");
                                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
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

                        #region ?????????????????? ?????????????????????? ??????????

                        #region ???????? ???? ??????????
                        if (!match.IsOvertime)
                        {
                            #region ?????????? ???????????? ?????????? ???????????? ????????????????

                            if (match.AScore + match.BScore == match.MaxRounds)
                            {
                                await RconHelper.SendCmd(rcon, "sm_msay " + $"{tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\n???????????? ?????????? ???????????????????? ????????-??????.\\n???????? ?????????????????????? ??????????????????????????.");
                                if (match.MatchType == 0)
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MATCH}");
                                }
                                else
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MIX}");
                                }
                                Thread.Sleep(2000);
                                await RconHelper.SendCmd(rcon, "sm_swap @all");
                                await RconHelper.SendCmd(rcon, "sv_pausable 1");
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

                            #region ?????????????????? ?????????? ?????? ???????????? ????????????

                            if (!match.FirstHalf)
                            {
                                if ((Math.Abs(match.AScore - match.BScore) >= 2) && (match.AScore == match.MaxRounds + 1 || match.BScore == match.MaxRounds + 1))
                                {
                                    await OnEndMatch(tags, result.WinningTeam, info, server);

                                    if (mapQueue.Count > 0 && isBestOfThree)
                                    {
                                        await RconHelper.SendMessage(rcon, $"???????????????????????????? ?????????? ?????????? ???? {mapQueue.FirstOrDefault().Trim()} ?????????? 5 ????????????!", Colors.legendary);
                                        Thread.Sleep(5000);
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
                                        await RconHelper.SendMessage(rcon, "????????????????!!!", Colors.crimson);
                                        await RconHelper.SendCmd(rcon, "sv_pausable 1");
                                        if (match.MatchType == 0)
                                        {
                                            await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MATCH_OVERTIME}");
                                        }
                                        else
                                        {
                                            await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MIX_OVERTIME}");
                                        }
                                        Thread.Sleep(2000);
                                        isResetFreezeTime = true;
                                        match.MaxRounds = 3;
                                    }
                                }
                            }

                            #endregion
                        }
                        #endregion
                        #region ??????????
                        else
                        {
                            #region ?????????? ???????????? ?? ????????????

                            if (match.AScoreOvertime + match.BScoreOvertime == match.MaxRounds && match.AScoreOvertime != match.BScoreOvertime)
                            {
                                await RconHelper.SendCmd(rcon, "sm_msay " + $"{tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\n???????????? ?????????? ???????????????????? ????????-??????.\\n???????? ?????????????????????? ??????????????????????????.");
                                if (match.MatchType == 0)
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MATCH_OVERTIME}");
                                }
                                else
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MIX_OVERTIME}");
                                }
                                Thread.Sleep(2000);
                                await RconHelper.SendCmd(rcon, "sm_swap @all");
                                await RconHelper.SendCmd(rcon, "sv_pausable 1");
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

                            #region ?????????????????? ?????????? ?????? ?????? ???????? ??????????

                            if ((Math.Abs(match.AScoreOvertime - match.BScoreOvertime) >= 2) && (match.AScoreOvertime == match.MaxRounds + 1 || match.BScoreOvertime == match.MaxRounds + 1))
                            {
                                await OnEndMatch(tags, result.WinningTeam, info, server);

                                if (mapQueue.Count > 0 && isBestOfThree)
                                {
                                    await RconHelper.SendMessage(rcon, $"???????????????????????????? ?????????? ?????????? ???? {mapQueue.FirstOrDefault().Trim()} ?????????? 5 ????????????!", Colors.legendary);
                                    Thread.Sleep(5000);
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
                                    await RconHelper.SendMessage(rcon, "????????????????!!!", Colors.crimson);
                                    await RconHelper.SendCmd(rcon, "sv_pausable 1");
                                    if (match.MatchType == 0)
                                    {
                                        await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MATCH_OVERTIME}");
                                    }
                                    else
                                    {
                                        await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MIX_OVERTIME}");
                                    }
                                    Thread.Sleep(2000);
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
                    if (chat.Channel == MessageChannel.All && mapNum > 0 && mapNum < 9 && currentMapSelector == chat.Player.Name)
                    {
                        if (mapPool.Count() == 1)
                        {
                            await RconHelper.SendMessage(rcon, $"?????? ????????, ?????????????? ?????????? ???????? ???? ?????????????? ?????? ??????????????????!", Colors.legendary);
                            return;
                        }
                        if (!mapPool.ContainsKey(mapNum))
                        {
                            await RconHelper.SendMessage(rcon, $"???? ?????????????? ??????????, ?????????????? ?????? ???????? ???????????????? ?????? ??????????????!", Colors.crimson);
                            return;
                        }

                        if (isBestOfOneStarted && mapPool.Count() != 1)
                        {
                            mapPool.Remove(mapNum);
                        }

                        if (isBestOfThree)
                        {
                            if (mapPool.Count() == 5 || mapPool.Count() == 6)
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
                                await RconHelper.SendMessage(rcon, $"?????????????? ??????????: {mapPool.FirstOrDefault().Value.Trim()}", Colors.legendary);
                                await RconHelper.SendMessage(rcon, $"???????????????????????????? ?????????? ?????????? ???? {mapPool.FirstOrDefault().Value.Trim()} ?????????? 5 ????????????!", Colors.legendary);
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
                                await RconHelper.SendMessage(rcon, $"???????????????? ???????????? ??????????: {mapPool.FirstOrDefault().Value.Trim()}", Colors.legendary);
                                await RconHelper.SendMessage(rcon, $"???????????????????????????? ?????????? ?????????? ???? {mapQueue.FirstOrDefault().Trim()} ?????????? 5 ????????????!", Colors.legendary);
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
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, ???????? ?????????????? ???????????? ??????????!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, ????????????????????, ???????????????? ?????????? ??????????, ?????????? ???????????????? ????!", Colors.legendary);
                            }
                        }

                        if (isBestOfThree)
                        {
                            if (mapPool.Count() == 5 || mapPool.Count() == 6)
                            {
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, ???????? ?????????????? ???????????? ??????????!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, ????????????????????, ???????????????? ?????????? ??????????, ?????????? ?????????????? ????!", Colors.legendary);
                            }
                            else
                            {
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, ???????? ?????????????? ???????????? ??????????!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, ????????????????????, ???????????????? ?????????? ??????????, ?????????? ???????????????? ????!", Colors.legendary);
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
                            await RconHelper.SendMessage(rcon, $"???????????? ???? ?????????? ????????????????????: {10 - info.MatchesPlayed}", Colors.ivory);
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!pause" && match.IsMatch)
                    {
                        if (!match.Pause)
                        {
                            if (match.TacticalPauses != 0)
                            {
                                await RconHelper.SendMessage(rcon, "???? ?????????????????? ???????????? ?????????? ?????????????????????? ???????????????? ??????????!", Colors.legendary);
                                await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                                match.TacticalPauses--;
                                match.Pause = true;
                                await RconHelper.SendMessage(rcon, $"?? ?????? ???????????????? {match.TacticalPauses} ?????????????????????? ??????????????????!", Colors.legendary);
                            }
                            else
                            {
                                await RconHelper.SendMessage(rcon, "???? ???????????? ???? ???????????? ?????????? ?????????????????????? ??????????????!", Colors.crimson);
                            }
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "???????????? ?????????????????? ?????????? ???? ?????????? ??????????!", Colors.crimson);
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!pause5" && match.IsMatch)
                    {
                        if (!match.Pause)
                        {
                            if (match.TechnicalPauses != 0)
                            {
                                await RconHelper.SendMessage(rcon, "???? ?????????????????? ???????????? ?????????? ?????????????????????? ???????????????????????? ??????????!", Colors.legendary);
                                await RconHelper.SendCmd(rcon, "mp_freezetime 300");
                                match.TechnicalPauses--;
                                match.Pause = true;
                                await RconHelper.SendMessage(rcon, $"?? ?????? ???????????????? {match.TechnicalPauses} ?????????????????????? ??????????????????!", Colors.legendary);
                            }
                            else
                            {
                                await RconHelper.SendMessage(rcon, "???? ???????????? ???? ???????????? ?????????? ?????????????????????? ??????????????!", Colors.crimson);
                            }
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "???????????? ?????????????????? ?????????? ???? ?????????? ??????????!", Colors.crimson);
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
                            Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
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

                        await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, ???????? ?????????????? ???????????? ??????????!");
                        await RconHelper.SendMessage(rcon, $"???????????? ???????????????? - {currentMapSelector}", Colors.crimson);
                        await RconHelper.SendMessage(rcon, $"????????????????????, ???????????????? ?????????? ??????????, ?????????? ???? ????????????????:", Colors.legendary);
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
                            Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
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

                        await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, ???????? ?????????????? ???????????? ??????????!");
                        await RconHelper.SendMessage(rcon, $"???????????? ???????????????? - {currentMapSelector}", Colors.crimson);
                        await RconHelper.SendMessage(rcon, $"????????????????????, ???????????????? ?????????? ??????????, ?????????? ???? ????????????????:", Colors.legendary);
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!ko3") && isCanBeginMatch && !match.KnifeRound)
                    {
                        if (OnlinePlayers.Count < match.MinPlayersToStart)
                        {
                            await RconHelper.SendCmd(rcon, "sm_msay ???????? ???? ?????????? ???????? ??????????????, ???????? ?????????????? ?????????? ????????????");
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
                            await RconHelper.SendCmd(rcon, "sm_msay ???????? ???? ?????????? ???????? ??????????????, ???????? ?????????????? ?????????? ????????????");
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
                            await RconHelper.LiveOnThree(rcon, match, OnlinePlayers);
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
                            //await RconHelper.SendMessage(rcon, "???? ?????????????? ???????????? ?????? ????????????????????????????!");
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
                        await RconHelper.SendMessage(rcon, "!ko3 - ?????????????????? ?????????????? ??????????", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!lo3 - ?????????????????? ????????", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!bo1 - ?????????????????? ?????????????????????? ?????? bo1 ??????????", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!bo3 - ?????????????????? ?????????????????????? ?????? bo3 ??????????", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!score - ?????????????? ???????? ??????????", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!cm - ?????????????????????????? ???????????????????? ????????", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!me - ?????????? ?????????? ????????????????????", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!pause - ?????????? ???????????????? ?????????????? (4 ???????? ???? ????????)", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!pause5 - ?????????? ???????????????????????? ?????????????? (2 ???????? ???? ????????)", Colors.ivory);
                        await RconHelper.SendMessage(rcon, "!recover - ?????????????????????????????? ???????? ?????????? ?????????? ?????????????? (???????? ??????????????????)", Colors.ivory);
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!check")
                    {
                        await RconHelper.SendCmd(rcon, "sm_vote \"???? ?????????????\"");
                    }

                    string[] chatWords = chat.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string word in chatWords)
                    {
                        if (ForbiddenWords.Contains(word.ToLower()))
                        {
                            if (chat.Channel == MessageChannel.All)
                            {
                                await RconHelper.SendCmd(rcon, $"kickid {chat.Player.ClientId} ???? ???????? ?????????????? ???? ?????????????????????????? ?????????????????????? ???????? ({word})");
                                await RconHelper.SendMessage(rcon, $"{chat.Player.Name} ?????? ???????????? ???? ?????????????????????????? ?????????????????????? ???????? ({word})", Colors.crimson);
                                break;
                            }
                            else
                            {
                                break;
                            }
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
                            await RconHelper.SendMessage(rcon, $"???????????? ???? ?????????? ????????????????????: {10 - info.MatchesPlayed}", Colors.ivory);
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
                                await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} ?????? ?????? ???????????????? ?????????????????????? ?????????? ({word})");
                                break;
                            }
                        }

                        if (!await ServerEvents.IsUserRegistered(connection.Player.SteamId))
                        {
                            await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} ?????? ?????????? ?????????????????? VK ?? ???????????? vk.com/im?sel=-55788587 (?????????????? !setid {connection.Player.SteamId})");
                        }

                        if (await ServerEvents.CheckIsBanned(connection.Player.SteamId))
                        {
                            await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} ???? ???????? ???????????????????????????? ???? ??????????????. ?????? ?????????????????? ???????????????????? ???????????????????? ?? ?????????????????????????? ??????????????.");
                        }

                        #region Connection Check

                        if (ConnectionController.Connections.Where(x => x.ClientId == connection.Player.ClientId).Any())
                        {
                            Connection connectionInfo = ConnectionController.Connections.Where(x => x.ClientId == connection.Player.ClientId).First();
                            connectionInfo.SteamId = connection.Player.SteamId;
                            var connectionCheckerResult = await ConnectionController.ExecuteChecker(connectionInfo);
                            if (connectionCheckerResult == 1)
                            {
                                await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} ?????????????????????????? VPN ??????????????????");
                            }
                            if (connectionCheckerResult == 2)
                            {
                                await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} ?????? ???????????????? ???????????? ???? ?????????????? ????-???? ???????????? IP Tables");
                            }

                            ConnectionController.RemoveItem(connectionInfo);
                        }
                        
                        #endregion

                        Logger.Print(server.ID, $"{connection.Player.Name} ({connection.Player.SteamId}) has been connected to {endpoint.Address}:{endpoint.Port}", LogLevel.Trace);
                    }
                });

                log.Listen<ChangeSides>(async result =>
                {
                    await RconHelper.SendCmd(rcon, "sm_swap @all");
                    await RconHelper.SendMessage(rcon, "???????????????? !lo3 ?????? ?????????????? ??????????!", Colors.mediumseagreen);
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
                            await RconHelper.SendMessage(rcon, "???????? ??????????????!", Colors.crimson);
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
                    await RconHelper.SendMessage(rcon, "???????????????? !lo3 ?????? ?????????????? ??????????!", Colors.mediumseagreen);
                });

                log.Listen<NameChange>(async result =>
                {
                    foreach (string word in ForbiddenWords)
                    {
                        if (result.NewName.ToLower().Contains(word))
                        {
                            await RconHelper.SendCmd(rcon, $"kickid {result.Player.ClientId} ?????? ?????? ???????????????? ?????????????????????? ?????????? ({word})");
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
                            await RconHelper.SendMessage(rcon, "???????? ?????? ??????????????!", Colors.crimson);
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
                            await RconHelper.SendMessage(rcon, $"???????????????????????????? ?????????? ?????????? ???? {mapQueue.FirstOrDefault().Trim()} ?????????? 5 ????????????!", Colors.legendary);
                            Thread.Sleep(5000);
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
                        //if (OnlinePlayers.Count() == 0 && NeedRestart)
                        //{
                        //    Logger.Print(server.ID, "Autorestart cuz players count is zero", LogLevel.Debug);
                        //    Environment.Exit(0);
                        //}
                    }
                });

                log.Listen<PlayerConnectedIPInfo>(async data =>
                {
                    await ServerEvents.InsertConnectData(Program.Node.ServerID, data);
                    ConnectionController.AddItem(new Connection()
                    {
                        ClientId = data.Player.ClientId,
                        Name = data.Player.Name,
                        SteamId = string.Empty,
                        IP = data.IP
                    });
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

                await RconHelper.SendCmd(rcon, $"sm_msay {tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\n???????? ????????????????!\\n?????????????????????? ?? ?????????????? ?????????????? {tags[winningTeam]}!\\n{tags[looser]}, ?? ?????????????????? ?????? ?????? ??????????????.");
                await RconHelper.SendMessage(rcon, "???????? ????????????????!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"?????????????????????? ?? ?????????????? ?????????????? {tags[winningTeam]}!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"{tags[looser]}, ?? ?????????????????? ?????? ?????? ??????????????.", Colors.mediumseagreen);
                await MatchEvents.InsertMatchLog(match.MatchId, $"<Match End>", info.Map, server.ID);
                await MatchEvents.InsertDemoName(match.MatchId, DemoName);
                MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], info.Map, server.ID, MatchPlayers, winningTeam, match);

                Match bMatch = match;

                MatchResultInfo matchResultInfo = new MatchResultInfo
                {
                    MapName = info.Map,
                    MatchScore = VKInteraction.Matches.GetMatchResult(bMatch.MatchId).Result,
                    MVPlayer = VKInteraction.Matches.GetMatchMVP(bMatch.MatchId).Result,
                    PlayerResults = VKInteraction.Matches.GetPlayerResults(bMatch.MatchId).Result
                };

                List<PlayerPictureData> playerPictures = new List<PlayerPictureData>();
                foreach (var player in MatchPlayers)
                {
                    Logger.Print(ServerID, $"{player.Name} - {player.Team}", LogLevel.Debug);
                    var data = await MatchEvents.GetPlayerResultData(player.SteamId, match);
                    data.Name = player.Name;
                    if (player.Team == winningTeam)
                    {
                        data.IsVictory = true;
                    }
                    else
                    {
                        data.IsVictory = false;
                    }
                    data.SteamId = player.SteamId;
                    playerPictures.Add(data);
                }

                Thread.Sleep(3000);

                await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                await RconHelper.SendCmd(rcon, "tv_stoprecord");
                match = new Match(0);
                isCanBeginMatch = true;
                Program.Node.FTPTools.UploadDemo(Program.Node.DemoName);
                if (!matchResultInfo.MatchScore.AName.Contains("Team ") && !matchResultInfo.MatchScore.BName.Contains("Team "))
                {
                    VKInteraction.Matches.PublishResult(matchResultInfo);
                }
                foreach (var data in playerPictures)
                {
                    VKInteraction.Matches.SendPlayerResult(data);
                    Thread.Sleep(1000);
                }
                OnlinePlayers.Clear();
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

                await RconHelper.SendCmd(rcon, $"sm_msay {tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\n???????? ????????????????!\\n?????????????????????? ?? ?????????????? ?????????????? {tags[winningTeam]}!\\n{tags[looser]}, ?? ?????????????????? ?????? ?????? ??????????????.");
                await RconHelper.SendMessage(rcon, "???????? ????????????????!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"?????????????????????? ?? ?????????????? ?????????????? {tags[winningTeam]}!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"{tags[looser]}, ?? ?????????????????? ?????? ?????? ??????????????.", Colors.mediumseagreen);
                await MatchEvents.InsertMatchLog(match.MatchId, $"<Match End>", mapName, server.ID);
                await MatchEvents.InsertDemoName(match.MatchId, DemoName);
                MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], mapName, server.ID, MatchPlayers, winningTeam, match);

                Match bMatch = match;

                MatchResultInfo matchResultInfo = new MatchResultInfo
                {
                    MapName = mapName,
                    MatchScore = VKInteraction.Matches.GetMatchResult(bMatch.MatchId).Result,
                    MVPlayer = VKInteraction.Matches.GetMatchMVP(bMatch.MatchId).Result,
                    PlayerResults = VKInteraction.Matches.GetPlayerResults(bMatch.MatchId).Result
                };

                List<PlayerPictureData> playerPictures = new List<PlayerPictureData>();
                foreach (var player in MatchPlayers)
                {
                    var data = await MatchEvents.GetPlayerResultData(player.SteamId, match);
                    data.Name = player.Name;
                    if (player.Team == winningTeam)
                    {
                        data.IsVictory = true;
                    }
                    else
                    {
                        data.IsVictory = false;
                    }
                    data.SteamId = player.SteamId;
                    playerPictures.Add(data);
                }

                Thread.Sleep(3000);

                await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                await RconHelper.SendCmd(rcon, "tv_stoprecord");
                match = new Match(0);
                isCanBeginMatch = true;
                Program.Node.FTPTools.UploadDemo(Program.Node.DemoName);
                if (!matchResultInfo.MatchScore.AName.Contains("Team ") && !matchResultInfo.MatchScore.BName.Contains("Team "))
                {
                    VKInteraction.Matches.PublishResult(matchResultInfo);
                }
                foreach (var data in playerPictures)
                {
                    VKInteraction.Matches.SendPlayerResult(data);
                    Thread.Sleep(500);
                }
                OnlinePlayers.Clear();
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
                await RconHelper.SendMessage(rcon, "?????????????? ??????????????: !ko3 !lo3 !bo1 !bo3 !me !cm !pause !pause5 !check", Colors.ivory);
                await RconHelper.SendMessage(rcon, "???????????????? !gethelp ?????? ?????????????????? ???????????????? ???????? ????????????", Colors.ivory);
                await RconHelper.SendMessage(rcon, "????????????????????: ???????????? ?????????? ?????????? ???????? ?????????????????????? ?????? ?????????????????? ????????", Colors.crimson);
#if DEBUG
                await RconHelper.SendMessage(rcon, "The process has been started in debug mode", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, "If you spot a bug please submit it to developer", Colors.mediumseagreen);
#endif
            }

            private async void CheckIsDeadMapVote()
            {
                Thread.Sleep(120 * 1000);
                if (isBestOfOneStarted)
                {
                    isBestOfOneStarted = !isBestOfOneStarted;
                    await RconHelper.SendMessage(rcon, "?????????????????????? bo1/bo3 ???????? ????????????????????????!", Colors.crimson);
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
                        //await RconHelper.SendCmd(rcon, "echo sended life packet");
                        if (OnlinePlayers.Count < match.MinPlayersToStop)
                        {
                            if (match.AScore == 0 && match.BScore == 0 || (match.AScore + match.BScore < 8))
                            {
                                await RconHelper.SendCmd(rcon, "tv_stoprecord");
                                await MatchEvents.ResetMatch(match.MatchId, server.ID);
                                await RconHelper.SendMessage(rcon, "???????? ??????????????!", Colors.crimson);
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
                                await RconHelper.SendMessage(rcon, $"???????????????????????????? ?????????? ?????????? ???? {mapQueue.FirstOrDefault().Trim()} ?????????? 5 ????????????!", Colors.legendary);
                                Thread.Sleep(5000);
                                await RconHelper.SendCmd(rcon, $"changelevel {mapQueue.FirstOrDefault().Trim()}");
                                mapQueue.Remove(mapQueue.FirstOrDefault());
                            }
                            else if (mapQueue.Count == 0)
                            {
                                isBestOfThree = false;
                            }
                        }
                    }
                    Thread.Sleep(120000);
                }
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "kTVCSS NODE LAUNCHER";
            Console.ForegroundColor = ConsoleColor.Green;
            Logger.Print(0, "Welcome, " + Environment.UserName, LogLevel.Info);

#if DEBUG

            args = new string[1];
            args[0] = "0";

#endif

            if (args.Length != 0)
            {
                Loader.LoadServers();

                ForbiddenWords.AddRange(File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "wordsfilter.txt"), System.Text.Encoding.UTF8));
                int id = int.Parse(args[0]);
                Console.Title = "[#" + ++id + "]" + " kTVCSS (v1.1b) @ " + Servers[int.Parse(args[0])].Host + ":" + Servers[int.Parse(args[0])].GamePort;

                Node node = new Node();
                Task.Run(async () => await node.StartNode(Servers[int.Parse(args[0])])).GetAwaiter().GetResult();
            }
        }
    }
}
