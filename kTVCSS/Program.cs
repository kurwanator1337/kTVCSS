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
using static kTVCSS.Game.Sourcemod;
using System.Timers;
using System.Runtime.ConstrainedExecution;
using System.Text;
using CoreRCON.Parsers.Csgo;
using Newtonsoft.Json;
using kTVCSS.Localization;
using Microsoft.Extensions.Logging;
using VkNet.Model.RequestParams;
using VkNet.Model;
using VkNet;
using kTVCSS.Game;
using System.Numerics;
using static System.Net.WebRequestMethods;
using System.Diagnostics.Metrics;
using System.Runtime.Intrinsics.Arm;

namespace kTVCSS
{
    class Program
    {
        public static Logger Logger = new Logger(0);
        public static ConfigTools ConfigTools = new ConfigTools();
        public static List<Server> Servers = new List<Server>();
        public static List<string> ForbiddenWords { get; set; } = new List<string>();
        public static List<MixMember> AllowedPlayers = new List<MixMember>();
        //public static List<Locale> Locales;
        //public static Locale CurrentLocale;
        private static string moduleVersion = "RC2.2";

        public class Node
        {
            public Node()
            {
                alertThread = new Thread(Alerter) { IsBackground = true };
            }

            private static Thread alertThread = null;
            private RCON rcon = null;
            private Match match = new Match(0, ServerType.Mix);
            private List<string> mapQueue = new List<string>();
            private Dictionary<int, string> mapPool = new Dictionary<int, string>();
            private static System.Timers.Timer aTimer;

            public static FTPTools FTPTools = null;
            public List<Player> MatchPlayers = null;
            public static List<PlayerRank> PlayersRank = new List<PlayerRank>();
            public static List<Player> OnlinePlayers = new List<Player>();
            public static int ServerID { get; set; } = 0;
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
            private static DateTime lastMatchStartAttemp = DateTime.Now;
            // mixes
            private static DateTime wannaMixStartDateTime = DateTime.Now;
            private static bool isMix = false;

            public async Task StartNode(Server server)
            {
                Logger.LoggerID = server.ID;

                //dynamic data = JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "dictionary.json")));

                //Locales = new List<Locale>();

                //foreach (var item in data.locales)
                //{
                //    Dictionary<string, string> values = new Dictionary<string, string>();

                //    foreach (var val in item.values)
                //    {
                //        values.Add(val.name.ToString(), val.value.ToString());
                //    }

                //    Locales.Add(new Locale(item.name.ToString(), values));
                //}

                //CurrentLocale = Locales.Where(x => x.Name == server.Language).FirstOrDefault();

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
                        Thread.Sleep(1000);
                    }
                }
                if (server.ServerType == ServerType.FastCup)
                {
                    Cvars.FREEZETIME_MATCH = 0;
                    Cvars.FREEZETIME_MIX = 0;
                    Cvars.HALF_TIME_PERIOD_MATCH = 3;
                    Cvars.HALF_TIME_PERIOD_MIX = 3;
                    Cvars.HALF_TIME_PERIOD_MIX_OVERTIME = 3;
                    Cvars.HALF_TIME_PERIOD_MATCH_OVERTIME = 3;
                }
                LogReceiver log = null;
                try
                {
                    log = new LogReceiver(server.NodePort, endpoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DEBUG NOT STARTED UDP LISTENER" + ex.ToString());
                    Environment.Exit(0);
                }
                ServerQueryPlayer[] players = await ServerQuery.Players(endpoint);
                var checkList = players.ToList();
                checkList.RemoveAll(x => x.Duration == -1);
                SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                FTPTools = new FTPTools(server);
                Logger.Print(server.ID, $"Created connection to {info.Name}", LogLevel.Trace);

                await RconHelper.SendMessage(rcon, $"Connection to kTVCSS processing node is established (version: {moduleVersion})", Colors.ivory);
#if DEBUG
                await RconHelper.SendMessage(rcon, "PROCESS STARTED IN DEBUG MODE", Colors.crimson);
#endif
                alertThread.Start(server);
                //await ServerEvents.SetServerFree(ServerID);

                var recoveredMatchID = await MatchEvents.CheckMatchLiveExists(server.ID);
                if (checkList.Count > 0 && recoveredMatchID == 0)
                {
                    await RconHelper.SendCmd(rcon, "sm_map " + info.Map);
                }
                //string test = string.Empty;
                //log.ListenRaw(test =>
                //{
                //    Console.WriteLine(test);
                //});

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

                        await MatchEvents.PlayerKill(kill.Killer.Name, kill.Killed.Name, kill.Killer.SteamId, kill.Killed.SteamId, hs, server.ID, match.MatchId, kill.Killer.Team);

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
                            $" {kill.Killed.Name}<{kill.Killed.SteamId}> with weapon <{kill.Weapon}> <{kill.Headshot}>", info.Map, server.ID, match);
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
                            await RconHelper.SendCmd(rcon, $"kickid {data.Player.ClientId} Please reconnect to the server.");
                            Logger.Print(Program.Node.ServerID, $"[FIX160723] [Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
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
                    using (SqlConnection sql = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                    {
                        await sql.OpenAsync();

                        foreach (var oPlayer in OnlinePlayers)
                        {
                            bool needKick = false;

                            using (SqlCommand command = new SqlCommand($"SELECT ANTICHEATREQUIRED FROM Players WITH (NOLOCK) WHERE STEAMID = '{oPlayer.SteamId}'", sql))
                            {
                                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        if (reader[0].ToString() == "1")
                                        {
                                            needKick = true;
                                        }
                                    }
                                }
                            }

                            if (needKick)
                            {
                                using (SqlCommand command = new SqlCommand($"SELECT STEAMID FROM AnticheatOnlineUsers WHERE STEAMID = '{oPlayer.SteamId}'", sql))
                                {
                                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                            if (reader[0].ToString() == oPlayer.SteamId)
                                            {
                                                needKick = false;
                                            }
                                        }
                                    }
                                }

                                if (needKick)
                                {
                                    await RconHelper.SendCmd(rcon, $"kickid {oPlayer.ClientId} anticheat is not runing!");
                                }
                            }
                        }
                    }
                        

                    if (match.IsMatch)
                    {
                        match.CanPause = true;

                        await RconHelper.SendCmd(rcon, "save_match");

                        match.PlayerKills.Clear();
                        match.OpenFragSteamID = string.Empty;

                        if (isResetFreezeTime)
                        {
                            match.IsNeedSetTeamScores = true;
                            await RconHelper.LiveOnThree(rcon, match, OnlinePlayers, server.ServerType);
                            isResetFreezeTime = !isResetFreezeTime;
                        }

                        if (match.Pause)
                        {
                            if (server.ServerType == ServerType.ClanMatch)
                            {
                                await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MATCH}");
                            }
                            else
                            {
                                await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MIX}");
                            }
                            match.Pause = !match.Pause;
                        }

                        await MatchEvents.InsertMatchLog(match.MatchId, $"<Round Start>", info.Map, server.ID, match);

                        #region Region tab names helper

                        var tgs = MatchEvents.GetTeamNames(MatchPlayers);
                        await RconHelper.SendCmd(rcon, $"se_scoreboard_teamname_t {tgs["TERRORIST"]}");
                        await RconHelper.SendCmd(rcon, $"se_scoreboard_teamname_ct {tgs["CT"]}");

                        #endregion


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
                                                if (server.ServerType == ServerType.ClanMatch)
                                                {
                                                    await RconHelper.SendMessage(rcon, "Set rules for the default match", Colors.crimson);
                                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MATCH}");
                                                    await RconHelper.SendCmd(rcon, $"mp_friendlyfire {Game.Cvars.FRIENDLYFIRE_MATCH}");

                                                    await RconHelper.SendMessage(rcon, $"You have 4 pauses available for 1 minute each (!pause)", Colors.mediumseagreen);
                                                    await RconHelper.SendMessage(rcon, $"And 2 pauses of 5 minutes (!pause5)", Colors.mediumseagreen);
                                                }
                                                else if (server.ServerType == ServerType.Mix)
                                                {
                                                    await RconHelper.SendMessage(rcon, "Set rules for the mix match", Colors.crimson);
                                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MIX}");
                                                    await RconHelper.SendCmd(rcon, $"mp_friendlyfire {Game.Cvars.FRIENDLYFIRE_MIX}");
                                                }
                                                await RconHelper.SendMessage(rcon, $"Average rating of the team {tags[tName]} - {ter}", Colors.crimson);
                                                await RconHelper.SendMessage(rcon, $"Average rating of the team {tags[ctName]} - {ct}", Colors.dodgerblue);
                                                await RconHelper.SendMessage(rcon, $"Check the match on: https://ktvcss.ru/match/{match.MatchId}", Colors.dodgerblue);
                                                string[] players = MatchPlayers.Select(x => x.SteamId).ToArray();


                                                //if (!tags[tName].Contains("Team ") && !tags[ctName].Contains("Team "))
                                                //{
                                                StringBuilder sb = new StringBuilder();
                                                sb.AppendLine($"Начинается матч на сервере №{ServerID}!");
                                                sb.AppendLine($"{tags[tName]} (Средний рейтинг: {ter}) vs {tags[ctName]} (Средний рейтинг: {ct})");
                                                sb.AppendLine($"Карта: {currentMapName}");
                                                sb.AppendLine($"Подробнее о матче: https://ktvcss.ru/match/{match.MatchId}");
                                                sb.AppendLine($"Список участников матча:");
                                                foreach (string player in players)
                                                {
                                                    sb.AppendLine(PlayerInfoMini.Get(player));
                                                }
                                                await VKInteraction.Matches.SendMessageToConf(sb.ToString());
                                                //}
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await RconHelper.SendMessage(rcon, "RulesetForMatch", Colors.crimson);
                                await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MATCH}");
                                await RconHelper.SendCmd(rcon, $"mp_friendlyfire {Game.Cvars.FRIENDLYFIRE_MATCH}");
                                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                            }
                        }
                    }
                });

                log.Listen<RoundEndScore>(async result =>
                {
                    if (match.IsMatch)
                    {
                        match.CanPause = false;

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

                        await MatchEvents.InsertMatchLog(match.MatchId, $"<Round End> {tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}", info.Map, server.ID, match);

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
                                            await rcon.SendCommandAsync($"ktv_mvp \"{MatchPlayers.Find(x => x.SteamId == player.Key).Name}\" \"made a triple kill!\"");
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a triple kill!", info.Map, server.ID, match);
                                            break;
                                        }
                                    case 4:
                                        {
                                            await rcon.SendCommandAsync($"ktv_mvp \"{MatchPlayers.Find(x => x.SteamId == player.Key).Name}\" \"made a quad kill!\"");
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a quad kill!", info.Map, server.ID, match);
                                            break;
                                        }
                                    case 5:
                                        {
                                            await rcon.SendCommandAsync($"sm_csay {MatchPlayers.Find(x => x.SteamId == player.Key).Name} RAMPAGE!!!");
                                            await rcon.SendCommandAsync($"ktv_mvp \"{MatchPlayers.Find(x => x.SteamId == player.Key).Name}\" \"MADE A RAMPAGE!!!\"");
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!", Colors.crimson);
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!", Colors.crimson);
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!", Colors.crimson);
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!", Colors.crimson);
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!", Colors.crimson);
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!", info.Map, server.ID, match);
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
                                await RconHelper.SendCmd(rcon, "sm_freeze @all");
                                await RconHelper.SendCmd(rcon, "sm_msay " + $"{tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\nThe timeout has been set! The match will continue automatically.");
                                if (server.ServerType == ServerType.ClanMatch)
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MATCH}");
                                }
                                else if (server.ServerType == ServerType.Mix)
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MIX}");
                                }
                                else
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime 3");
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

                            #region Окончание матча или начало оверов

                            if (!match.FirstHalf)
                            {
                                if ((Math.Abs(match.AScore - match.BScore) >= 2) && (match.AScore == match.MaxRounds + 1 || match.BScore == match.MaxRounds + 1))
                                {
                                    await OnEndMatch(tags, result.WinningTeam, currentMapName, server);

                                    if (mapQueue.Count > 0 && isBestOfThree)
                                    {
                                        await RconHelper.SendMessage(rcon, $"Auto-change to the map {mapQueue.FirstOrDefault().Trim()} after 60 seconds!", Colors.legendary);
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
                                        await RconHelper.SendMessage(rcon, "Overtime!!!", Colors.crimson);
                                        await RconHelper.SendCmd(rcon, "sv_pausable 1");
                                        if (server.ServerType == ServerType.ClanMatch)
                                        {
                                            await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MATCH_OVERTIME}");
                                        }
                                        else if (server.ServerType == ServerType.Mix)
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
                        #region Оверы
                        else
                        {
                            #region Смена сторон в оверах

                            if (match.AScoreOvertime + match.BScoreOvertime == match.MaxRounds && match.AScoreOvertime != match.BScoreOvertime)
                            {
                                await RconHelper.SendCmd(rcon, "sm_freeze @all");
                                await RconHelper.SendCmd(rcon, "sm_msay " + $"{tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\nThe timeout has been set! The match will continue automatically.");
                                if (server.ServerType == ServerType.ClanMatch)
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MATCH_OVERTIME}");
                                }
                                else if (server.ServerType == ServerType.Mix)
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MIX_OVERTIME}");
                                }
                                else
                                {
                                    await RconHelper.SendCmd(rcon, $"mp_freezetime 3");
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

                            #region Окончание матча или еще одни оверы

                            if ((Math.Abs(match.AScoreOvertime - match.BScoreOvertime) >= 2) && (match.AScoreOvertime == match.MaxRounds + 1 || match.BScoreOvertime == match.MaxRounds + 1))
                            {
                                await OnEndMatch(tags, result.WinningTeam, currentMapName, server);

                                if (mapQueue.Count > 0 && isBestOfThree)
                                {
                                    await RconHelper.SendMessage(rcon, $"Auto-change to the map {mapQueue.FirstOrDefault().Trim()} after 60 seconds!", Colors.legendary);
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
                                    await RconHelper.SendMessage(rcon, "Overtime!!!", Colors.crimson);
                                    await RconHelper.SendCmd(rcon, "sv_pausable 1");
                                    if (server.ServerType == ServerType.ClanMatch)
                                    {
                                        await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.HALF_TIME_PERIOD_MATCH_OVERTIME}");
                                    }
                                    else if (server.ServerType == ServerType.Mix)
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
                        isCanBeginMatch = true;
                    }
                    await rcon.SendCommandAsync($"score_set {match.BScore} {match.AScore}");
                });

                log.Listen<ChatMessage>(async chat =>
                {
                    await ServerEvents.InsertChatMessage(chat.Player.SteamId, chat.Message, ServerID);
                    if (match.IsMatch)
                    {
                        await MatchEvents.InsertMatchLog(match.MatchId, $"{chat.Player.Name} <{chat.Player.SteamId}> said: {chat.Message}", info.Map, server.ID, match);
                    }
                    int.TryParse(chat.Message, out int mapNum);
                    if (chat.Channel == MessageChannel.All && mapNum > 0 && mapNum <= 9 && currentMapSelector == chat.Player.Name)
                    {
                        if (mapPool.Count() == 1)
                        {
                            await RconHelper.SendMessage(rcon, "There are no maps that could be selected or banned!", Colors.legendary);
                            return;
                        }
                        if (!mapPool.ContainsKey(mapNum))
                        {
                            await RconHelper.SendMessage(rcon, "You have selected a map that has already been banned or selected!", Colors.crimson);
                            return;
                        }

                        if (isBestOfOneStarted && mapPool.Count() != 1)
                        {
                            mapPool.Remove(mapNum);
                        }

                        if (isBestOfThree)
                        {
                            if (mapPool.Count() == 4 || mapPool.Count() == 5)
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
                                await RconHelper.SendMessage(rcon, $"Selected map: {mapPool.FirstOrDefault().Value.Trim()}", Colors.legendary);
                                await RconHelper.SendMessage(rcon, $"Auto-change to the map {mapPool.FirstOrDefault().Value.Trim()}...", Colors.legendary);
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
                                await RconHelper.SendMessage(rcon, $"The decisive map: {mapPool.FirstOrDefault().Value.Trim()}", Colors.legendary);
                                await RconHelper.SendMessage(rcon, $"Auto-change to the map {mapQueue.FirstOrDefault().Trim()}...", Colors.legendary);
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
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, it's your turn to ban the map!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, please write the map's number to ban it!", Colors.legendary);
                            }
                        }

                        if (isBestOfThree)
                        {
                            if (mapPool.Count() == 4 || mapPool.Count() == 5)
                            {
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, it's your turn to pick the map!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, please write the map's number to pick it!", Colors.legendary);
                            }
                            else
                            {
                                await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, it's your turn to ban the map!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, please write the map's number to ban it!", Colors.legendary);
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
                            await RconHelper.SendMessage(rcon, $"Matches until the end of calibration: {10 - info.MatchesPlayed}", Colors.ivory);
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!pause" && match.IsMatch)
                    {
                        if (server.ServerType != ServerType.Mix)
                        {
                            if (!match.Pause)
                            {
                                if (!match.CanPause)
                                {
                                    await RconHelper.SendMessage(rcon, "You can't take a pause at the moment. Write this command during the round, not at the end/beginning of the round.", Colors.crimson);
                                    return;
                                }
                                if (match.TacticalPauses != 0)
                                {
                                    await RconHelper.SendMessage(rcon, "At the end of the round, a one-minute pause will be set!", Colors.legendary);
                                    await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                                    match.TacticalPauses--;
                                    match.Pause = true;
                                    await RconHelper.SendMessage(rcon, $"The number of remaining tactical breaks: {match.TacticalPauses}", Colors.legendary);
                                }
                                else
                                {
                                    await RconHelper.SendMessage(rcon, "You can't take a tactical break anymore!", Colors.crimson);
                                }
                            }
                            else
                            {
                                await RconHelper.SendMessage(rcon, "You can't require a break during a break!", Colors.crimson);
                            }
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!pause5" && match.IsMatch)
                    {
                        if (server.ServerType != ServerType.Mix)
                        {
                            if (!match.Pause)
                            {
                                if (!match.CanPause)
                                {
                                    await RconHelper.SendMessage(rcon, "You can't take a pause at the moment. Write this command during the round, not at the end/beginning of the round.", Colors.crimson);
                                    return;
                                }
                                if (match.TechnicalPauses != 0)
                                {
                                    await RconHelper.SendMessage(rcon, "At the end of the round, a five-minute pause will be set!", Colors.legendary);
                                    await RconHelper.SendCmd(rcon, "mp_freezetime 300");
                                    match.TechnicalPauses--;
                                    match.Pause = true;
                                    await RconHelper.SendMessage(rcon, $"The number of remaining technical breaks: {match.TechnicalPauses}", Colors.legendary);
                                }
                                else
                                {
                                    await RconHelper.SendMessage(rcon, "You can't take a technical break anymore!", Colors.crimson);
                                }
                            }
                            else
                            {
                                await RconHelper.SendMessage(rcon, "You can't require a break during a break!", Colors.crimson);
                            }
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!bo3") && !isBestOfOneStarted && !isBestOfThree && !match.IsMatch && isCanBeginMatch && !match.KnifeRound)
                    {
                        if (server.ServerType == ServerType.Mix) return;
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

                        await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, it's your turn to ban the map!");
                        await RconHelper.SendMessage(rcon, $"The first one - {currentMapSelector}", Colors.crimson);
                        await RconHelper.SendMessage(rcon, "please write the map's number to ban it!", Colors.legendary);
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!bo1") && !isBestOfOneStarted && !isBestOfThree && !match.IsMatch && isCanBeginMatch && !match.KnifeRound)
                    {
                        if (server.ServerType == ServerType.Mix) return;
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

                        await RconHelper.SendCmd(rcon, $"sm_hsay {currentMapSelector}, it's your turn to ban the map!");
                        await RconHelper.SendMessage(rcon, $"The first one - {currentMapSelector}", Colors.crimson);
                        await RconHelper.SendMessage(rcon, "please write the map's number to ban it!", Colors.legendary);
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!ko3") && isCanBeginMatch && !match.KnifeRound && server.ServerType == ServerType.ClanMatch)
                    {
                        if (OnlinePlayers.Count < match.MinPlayersToStart)
                        {
                            await RconHelper.SendCmd(rcon, $"sm_msay The match cannot be started until there are less than eight players");
                            return;
                        }
                        if (!match.IsMatch)
                        {
                            match.KnifeRound = true;
                            lastMatchStartAttemp = DateTime.Now;
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_knives_start.cfg");
                            await RconHelper.Knives(rcon, match, server.ServerType);
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!lo3") && isCanBeginMatch && !match.KnifeRound)
                    {
                        //if (DateTime.Now.Hour >= 5 && DateTime.Now.Hour <= 7)
                        //{
                        //    await RconHelper.SendMessage(rcon, "Пацаны, идите спите, потом ныть будете, что не выспались", Colors.crimson);
                        //    return;
                        //}
                        
                        if (server.ServerType == ServerType.Mix)
                        {
                            if (!match.KnifeRound)
                            {
                                if (DateTime.Now.Subtract(lastMatchStartAttemp).TotalMinutes >= 1)
                                {
                                    isCanBeginMatch = false;

                                    wannaMixStartDateTime = wannaMixStartDateTime.AddMinutes(5);

                                    if (OnlinePlayers.Count == 10)
                                    {
                                        Dictionary<string, int> pList = new Dictionary<string, int>();

                                        using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                                        {
                                            await connection.OpenAsync();
                                            SqlCommand query = new SqlCommand($"SELECT STEAMID, TEAM FROM MixesMembers AS MM INNER JOIN Mixes AS M ON M.GUID = MM.GUID WHERE SERVERID = {ServerID}", connection);
                                            using (var reader = await query.ExecuteReaderAsync())
                                            {
                                                while (await reader.ReadAsync())
                                                {
                                                    pList.TryAdd(reader[0].ToString(), int.Parse(reader[1].ToString()));
                                                }
                                            }
                                        }

                                        foreach (var player in pList)
                                        {
                                            var team = player.Value;

                                            if (team == 0)
                                            {
                                                await RconHelper.SendCmd(rcon, $"playerswitch {OnlinePlayers.Where(x => x.SteamId == player.Key).FirstOrDefault().ClientId} 2");
                                            }
                                            else
                                            {
                                                await RconHelper.SendCmd(rcon, $"playerswitch {OnlinePlayers.Where(x => x.SteamId == player.Key).FirstOrDefault().ClientId} 3");
                                            }

                                            Thread.Sleep(100);
                                        }

                                        Thread.Sleep(1500);
                                    }

                                    await RconHelper.SendMessage(rcon, "Before starting the mix, you need to play a knife round", Colors.ivory);
                                    await RconHelper.SendMessage(rcon, "ATTENTION! YOU HAVE ONE MUNUTE TO PLAY THE KNIVES OR ITS WILL BE RESTARTED!!!", Colors.crimson);
                                    Thread.Sleep(1500);
                                    lastMatchStartAttemp = DateTime.Now.AddMinutes(1);
                                    await RconHelper.SendCmd(rcon, "exec ktvcss/on_knives_start.cfg");
                                    await RconHelper.Knives(rcon, match, server.ServerType);
                                    return;
                                }
                            }
                        }
                        if (OnlinePlayers.Count < match.MinPlayersToStart)
                        {
                            await RconHelper.SendCmd(rcon, $"sm_msay The match cannot be started until there are less than eight players");
                            return;
                        }
                        // проверка на нормальность составов команд
                        if (server.ServerType == ServerType.ClanMatch)
                        {
                            await RconHelper.SendMessage(rcon, "Wait, there is a check on the possibility of starting the match...", Colors.turquoise);

                            List<TeamMember> members = new List<TeamMember>();

                            foreach (var player in OnlinePlayers)
                            {
                                using (SqlConnection sql = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                                {
                                    sql.Open();
                                    using (SqlCommand cmd = new SqlCommand($"SELECT NAME FROM Teams INNER JOIN TeamsMembers ON TeamsMembers.TEAMID = Teams.ID WHERE STEAMID = '{player.SteamId}'", sql))
                                    {
                                        using (SqlDataReader reader = cmd.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                members.Add(new TeamMember()
                                                {
                                                    SteamId = player.SteamId,
                                                    TeamName = reader[0].ToString()
                                                });
                                            }
                                        }
                                    }
                                }
                            }

                            IEnumerable<string> distinctTeams = members.Select(x => x.TeamName).Distinct();
                            foreach (string team in distinctTeams)
                            {
                                var playersOfTeamX = members.Where(x => x.TeamName == team);
                                if (playersOfTeamX.Count() < 4 && playersOfTeamX.Count() > 1)
                                {
                                    await RconHelper.SendMessage(rcon, $"The match cannot be started because the team {team} contains too many stand-ins!", Colors.legendary);
                                    return;
                                }
                            }
                        }
                        if (!match.IsMatch)
                        {
                            isCanBeginMatch = false;
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_start.cfg");
                            SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                            currentMapName = info.Map;
                            DemoName = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss") + "_" + info.Map;
                            await RconHelper.SendCmd(rcon, "tv_record " + DemoName);
                            match = new Match(15, server.ServerType);
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
                            await RconHelper.LiveOnThree(rcon, match, OnlinePlayers, server.ServerType);
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!recover") && isCanBeginMatch)
                    {
                        // not done
                        return;
                        //var recoveredMatchID = await MatchEvents.CheckMatchLiveExists(server.ID);
                        //if (recoveredMatchID != 0)
                        //{
                        //    //await RconHelper.SendMessage(rcon, "RECOVERED LIVE");
                        //    MatchPlayers = new List<Player>();
                        //    MatchPlayers.AddRange(OnlinePlayers);
                        //    match = new Match(15);
                        //    match.MatchId = recoveredMatchID;
                        //    match = await MatchEvents.GetLiveMatchResults(server.ID, match);
                        //    match.RoundID = match.AScore + match.BScore;
                        //    if (match.AScore + match.BScore >= match.MaxRounds)
                        //    {
                        //        match.FirstHalf = false;
                        //    }
                        //    //await RconHelper.SendMessage(rcon, $"{tName} [{match.AScore}-{match.BScore}] {ctName}");
                        //    foreach (var player in MatchPlayers)
                        //    {
                        //        await ServerEvents.AuthPlayer(player.SteamId, player.Name);
                        //    }
                        //}
                        //else
                        //{
                        //    //await RconHelper.SendMessage(rcon, "Не найдено матчей для восстановления!");
                        //}
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!score") && match.IsMatch)
                    {
                        SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                        var tags = MatchEvents.GetTeamNames(MatchPlayers);
                        await RconHelper.SendCmd(rcon, "sys_say {crimson}" + tags[tName] + " {ivory}[" + match.AScore + "-" + match.BScore + "]{dodgerblue} " + tags[ctName]);
                    }

                    foreach (string forbiddenWord in ForbiddenWords)
                    {
                        if (chat.Message.Contains(forbiddenWord))
                        {
                            try
                            {
                                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                                {
                                    await connection.OpenAsync();

                                    SqlCommand query = new SqlCommand("[BanPlayerBySteam]", connection)
                                    {
                                        CommandType = System.Data.CommandType.StoredProcedure
                                    };

                                    query.Parameters.AddWithValue("@STEAMID", chat.Player.SteamId);
                                    query.Parameters.AddWithValue("@REASON", chat.Message);

                                    await query.ExecuteNonQueryAsync();
                                }
                            }
                            catch (Exception)
                            {
                                //
                            }

                            await RconHelper.SendCmd(rcon, $"kickid {chat.Player.ClientId} You have been blocked for toxicity");
                            await RconHelper.SendMessage(rcon, $"{chat.Player.Name} have been blocked for toxicity", Colors.crimson);

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
                        //await ServerEvents.InsertConnectData(ServerID, connection);

                        try
                        {
                            if (match.IsMatch)
                            {
                                foreach (var dp in match.DisconnectedPlayers)
                                {
                                    if (DateTime.Now.Subtract(dp.Value).Minutes >= 5)
                                    {
                                        try
                                        {
                                            using (SqlConnection sql = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                                            {
                                                await sql.OpenAsync();

                                                SqlCommand query = new SqlCommand("[BanPlayerBySteam]", sql)
                                                {
                                                    CommandType = System.Data.CommandType.StoredProcedure
                                                };

                                                query.Parameters.AddWithValue("@STEAMID", dp.Key);
                                                query.Parameters.AddWithValue("@REASON", "You got banned for leaving the mix! Ask an administration for details.");

                                                await query.ExecuteNonQueryAsync();
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            //
                                        }
                                    }
                                }
                                match.DisconnectedPlayers.Clear();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Print(ServerID, ex.ToString(), LogLevel.Error);
                        }

                        if (info.IsCalibration == 0)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {connection.Player.Name} [{info.MMR} MMR]", Colors.ivory);
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%", Colors.ivory);
                        }

                        if (info.IsCalibration == 1)
                        {
                            await RconHelper.SendMessage(rcon, $"[{info.RankName}] {connection.Player.Name}", Colors.ivory);
                            await RconHelper.SendMessage(rcon, $"KDR: {Math.Round(info.KDR, 2)}, HSR: {Math.Round(info.HSR, 2)}, AVG: {Math.Round(info.AVG, 2)}, WinRate: {Math.Round(info.WinRate, 2)}%", Colors.ivory);
                            await RconHelper.SendMessage(rcon, $"Matches until the end of calibration: {10 - info.MatchesPlayed}", Colors.ivory);
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

                        if (!await ServerEvents.IsUserRegistered(connection.Player.SteamId))
                        {
                            await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} You need to link your VK to our project group. Open https://vk.com/im?sel=-55788587 and write !setid {connection.Player.SteamId}");
                        }

                        var banCheckResult = await ServerEvents.CheckIsBanned(connection.Player.SteamId);

                        if (banCheckResult.FirstOrDefault().Key)
                        {
                            await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} You have been blocked on the project. Reason: {banCheckResult.FirstOrDefault().Value}");
                        }

                        #region Connection Check

                        if (ConnectionController.Connections.Where(x => x.ClientId == connection.Player.ClientId).Any())
                        {
                            Connection connectionInfo = ConnectionController.Connections.Where(x => x.ClientId == connection.Player.ClientId).First();
                            connectionInfo.SteamId = connection.Player.SteamId;
                            var connectionCheckerResult = await ConnectionController.ExecuteChecker(connectionInfo);
                            if (connectionCheckerResult == 1)
                            {
                                await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} Usage of the VPN is prohibited");
                            }
                            if (connectionCheckerResult == 2)
                            {
                                await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} You are denied access to the servers due to blocking your subnet");
                            }

                            ConnectionController.RemoveItem(connectionInfo);
                        }

                        #endregion

                        Logger.Print(server.ID, $"{connection.Player.Name} ({connection.Player.SteamId}) has been connected to {endpoint.Address}:{endpoint.Port}", LogLevel.Trace);

                        if (!await ServerEvents.IsUserTeamMember(connection.Player.SteamId) && server.ServerType == ServerType.ClanMatch)
                        {
                            await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} You are not a member of any team! Create or join the team: https://ktvcss.ru");
                        }

                        // checking mixes
                        if (server.ServerType == ServerType.Mix)
                        {
                            string map = string.Empty;
                            using (SqlConnection sql = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                            {
                                await sql.OpenAsync();
                                SqlCommand query = new SqlCommand($"SELECT STEAMID FROM MixesAllowedPlayers WHERE SERVERID = {ServerID}", sql);
                                using (SqlDataReader reader = query.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        string steam = reader[0].ToString();
                                        if (!AllowedPlayers.Where(x => x.SteamID == steam).Any())
                                        {
                                            AllowedPlayers.Add(new MixMember() { SteamID = steam });
                                        }
                                    }
                                }
                            }

                            using (SqlConnection sql = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                            {
                                await sql.OpenAsync();
                                SqlCommand query = new SqlCommand($"SELECT MAP FROM Mixes WHERE SERVERID = {ServerID}", sql);
                                using (SqlDataReader reader = query.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        map = reader[0].ToString();
                                    }
                                }
                            }

                            if (currentMapName != map)
                            {
                                await RconHelper.SendCmd(rcon, "changelevel " + map);
                            }

                            if (AllowedPlayers.Count > 0)
                            {
                                if (!isMix)
                                {
                                    wannaMixStartDateTime = DateTime.Now.AddMinutes(5);
                                }

                                isMix = true;
                            }
                            else
                            {
                                isMix = false;
                            }

                            if (!AllowedPlayers.Where(x => x.SteamID == connection.Player.SteamId).Any())
                            {
                                await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} you are don't participate the mix! https://ktvcss.ru/mixes");
                            }
                            else
                            {
                                AllowedPlayers.FirstOrDefault(x => x.SteamID == connection.Player.SteamId).Joined = true;
                            }
                        }

                        // anticheat
                        bool needKick = false;
                        using (SqlConnection sql = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                        {
                            await sql.OpenAsync();

                            using (SqlCommand command = new SqlCommand($"SELECT ANTICHEATREQUIRED FROM Players WHERE STEAMID = '{connection.Player.SteamId}'", sql))
                            {
                                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                                {
                                    while (await reader.ReadAsync())
                                    {
                                        if (reader[0].ToString() == "1")
                                        {
                                            needKick = true;
                                        }
                                    }
                                }
                            }

                            if (needKick)
                            {
                                using (SqlCommand command = new SqlCommand($"SELECT STEAMID FROM AnticheatOnlineUsers WHERE STEAMID = '{connection.Player.SteamId}'", sql))
                                {
                                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                            if (reader[0].ToString() == connection.Player.SteamId)
                                            {
                                                needKick = false;
                                            }
                                        }
                                    }
                                }
                                
                                if (needKick)
                                {
                                    await RconHelper.SendCmd(rcon, $"kickid {connection.Player.ClientId} anticheat is not runing!");
                                }
                            }
                        }
                    }
                });

                log.Listen<ChangeSides>(async result =>
                {
                    await RconHelper.SendCmd(rcon, "sm_swap @all");
                    await RconHelper.SendMessage(rcon, "When you are ready write !lo3 to start the match!", Colors.mediumseagreen);
                });

                log.Listen<MapChange>(async result =>
                {
                    Logger.Print(server.ID, $"Map change to {result.Map}", LogLevel.Trace);

                    currentMapName = result.Map;

                    if (match.IsMatch)
                    {
                        if (match.AScore == 0 && match.BScore == 0 || (match.AScore + match.BScore < 8))
                        {
                            if (server.ServerType == ServerType.Mix)
                            {
                                isMix = false;
                                AllowedPlayers.Clear();
                                await MatchEvents.DeleteMix(ServerID);
                            }
                            await RconHelper.SendCmd(rcon, "tv_stoprecord");
                            await MatchEvents.ResetMatch(match.MatchId, server.ID);
                            await RconHelper.SendMessage(rcon, "The match has been canceled!", Colors.crimson);
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                            await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                            isCanBeginMatch = true;
                            match = new Match(0, server.ServerType);
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
                    await RconHelper.SendMessage(rcon, "When you are ready write !lo3 to start the match!", Colors.mediumseagreen);
                });

                log.Listen<NameChange>(async result =>
                {
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
                    if (server.ServerType == ServerType.Mix)
                    {
                        await RconHelper.SendMessage(rcon, "На миксах команда !cm запрещена!", Colors.crimson);
                        return;
                    }

                    if (match.IsMatch)
                    {
                        if (match.AScore == 0 && match.BScore == 0 || (match.AScore + match.BScore < 8))
                        {
                            if (server.ServerType == ServerType.Mix)
                            {
                                isMix = false;
                                AllowedPlayers.Clear();
                                await MatchEvents.DeleteMix(ServerID);
                            }
                            await RconHelper.SendCmd(rcon, "tv_stoprecord");
                            await MatchEvents.ResetMatch(match.MatchId, server.ID);
                            await RconHelper.SendMessage(rcon, "The match has been canceled!", Colors.crimson);
                            Thread.Sleep(3000);
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                            await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                            isCanBeginMatch = true;
                            match = new Match(0, server.ServerType);
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

                        await OnEndMatch(tags, winner, currentMapName, server);

                        if (mapQueue.Count > 0 && isBestOfThree)
                        {
                            await RconHelper.SendMessage(rcon, $"Auto-change to the map {mapQueue.FirstOrDefault().Trim()} after 60 seconds!", Colors.legendary);
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
                        //await ServerEvents.InsertDisconnectData(ServerID, connection);
                        if (match.IsMatch)
                        {
                            if (match.IsNeedPauseOnPlayerTimeOut && server.ServerType == ServerType.ClanMatch)
                            {
                                if (connection.Reason.Contains("timed out"))
                                {
                                    await RconHelper.SendMessage(rcon, "At the end of the round, a two-minute pause will be set, because the player has lost the connection!", Colors.legendary);
                                    await RconHelper.SendCmd(rcon, "mp_freezetime 120");
                                    match.Pause = true;
                                    match.IsNeedPauseOnPlayerTimeOut = false;
                                }
                            }

                            if (server.ServerType == ServerType.Mix)
                            {
                                if (connection.Reason.Contains("Disconnect by ClientMod"))
                                {
                                    await RconHelper.SendMessage(rcon, connection.Player.Name + " " + "will be banned in 5 mins for leaving the match!", Colors.legendary);
                                    if (!match.DisconnectedPlayers.Where(x => x.Key == connection.Player.SteamId).Any())
                                        match.DisconnectedPlayers.Add(connection.Player.SteamId, DateTime.Now);
                                    else
                                    {
                                        match.DisconnectedPlayers[connection.Player.SteamId] = DateTime.Now;
                                    }
                                }
                            }
                        }

                        if (server.ServerType == ServerType.Mix)
                        {
                            try
                            {
                                AllowedPlayers.FirstOrDefault(x => x.SteamID == connection.Player.SteamId).Joined = false;
                            }
                            catch (Exception)
                            {
                                //
                            }
                        }
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

                    //if (match.IsMatch)
                    //{
                    //    try
                    //    {
                    //        using (SqlConnection con = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                    //        {
                    //            await con.OpenAsync();
                    //            SqlCommand query = new SqlCommand($"SELECT FRAGS, DEATHS FROM MatchesBackups WHERE ID = {match.MatchId}" +
                    //                $" AND STEAMID = '{data.Player.SteamId}'", con);
                    //            using (var reader = await query.ExecuteReaderAsync())
                    //            {
                    //                while (await reader.ReadAsync())
                    //                {
                    //                    await Task.Delay(3000);
                    //                    await RconHelper.SendCmd(rcon, $"player_score_set {data.Player.SteamId.Replace(":", "|")} " +
                    //                        $"{reader[0].ToString()} {reader[1].ToString()}");
                    //                }
                    //            }
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                    //    }
                    //}
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

                log.Listen<DamageEvent>(async data =>
                {
                    Console.WriteLine(data.Damage);
                });

                await Task.Delay(-1);
            }

            private async Task OnEndMatch(Dictionary<string, string> tags, string winningTeam, string mapName, Server server)
            {
                if (server.ServerType == ServerType.Mix)
                {
                    isMix = false;
                    AllowedPlayers.Clear();
                    await MatchEvents.DeleteMix(ServerID);
                }

                string looser = string.Empty;

                if (winningTeam == tName)
                {
                    looser = ctName;
                }
                else
                {
                    looser = tName;
                }

                await RconHelper.SendCmd(rcon, $"sm_msay {tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}\\nThe match has been finished!\\nCongratulations on the victory of the team {tags[winningTeam]}!\\n{tags[looser]}, good luck next time!");
                await RconHelper.SendMessage(rcon, "The match has been finished!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"Congratulations on the victory of the team {tags[winningTeam]}!", Colors.mediumseagreen);
                await RconHelper.SendMessage(rcon, $"{tags[looser]}, good luck next time!", Colors.mediumseagreen);
                await MatchEvents.InsertMatchLog(match.MatchId, $"<Match End>", mapName, server.ID, match);
                await MatchEvents.InsertDemoName(match.MatchId, DemoName);
                MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], mapName, server.ID, MatchPlayers, winningTeam, match);

                //if (!tags[tName].Contains("Team ") && !tags[ctName].Contains("Team "))
                //{

                //}

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

                try
                {
                    await VKInteraction.Matches.SendMessageToConf($"Закончился матч на сервере №{ServerID}\r\n\r\n" +
                                               $"{tags[tName]} [{bMatch.AScore}-{bMatch.BScore}] {tags[ctName]}\r\n\r\nКарта: {currentMapName}\r\n\r\nДлительность матча составила: {bMatch.Stopwatch.Elapsed.ToString(@"hh\:mm\:ss")}\r\n\r\nПодробнее о матче на сайте: https://ktvcss.ru/match/{bMatch.MatchId}");
                }
                catch (Exception ex)
                {
                    Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                }

                await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                await RconHelper.SendCmd(rcon, "tv_stoprecord");
                int matchId = match.MatchId;

                var disconnectedPlayers = match.DisconnectedPlayers;
                int mp = MatchPlayers.Count;
                if (server.ServerType == ServerType.Mix && mp <= 10)
                {
                    DateTime now = DateTime.Now;
                    foreach (var item in disconnectedPlayers)
                    {
                        if (now.Subtract(item.Value).Minutes >= 5)
                        {
                            try
                            {
                                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                                {
                                    await connection.OpenAsync();

                                    SqlCommand query = new SqlCommand("[BanPlayerBySteam]", connection)
                                    {
                                        CommandType = System.Data.CommandType.StoredProcedure
                                    };

                                    query.Parameters.AddWithValue("@STEAMID", item.Key);
                                    query.Parameters.AddWithValue("@REASON", "You got banned for leaving the mix! Ask an administration for details.");

                                    await query.ExecuteNonQueryAsync();
                                }
                            }
                            catch (Exception)
                            {
                                //
                            }
                        }
                    }
                }

                match = new Match(0, server.ServerType);
                isCanBeginMatch = true;
                Program.Node.FTPTools.UploadDemo(Program.Node.DemoName);
                try
                {
                    if (!tags[tName].Contains("Team ") && !tags[ctName].Contains("Team "))
                    {
                        VKInteraction.Matches.PublishResult(matchResultInfo, matchId);
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                }
                foreach (var data in playerPictures)
                {
                    VKInteraction.Matches.SendPlayerResult(data, matchId);
                    Thread.Sleep(1000);
                }
                OnlinePlayers.Clear();

                if (server.ServerType == ServerType.Mix)
                {
                    Environment.Exit(0);
                }

                if (server.ServerType == ServerType.ClanMatch)
                {
                    if (!isBestOfThree)
                    {
                        Environment.Exit(0);
                    }
                }
            }

            private void Rcon_OnDisconnected()
            {
                Logger.Print(ServerID, "RCON connection is closed", LogLevel.Warn);
                if (!match.IsMatch)
                {
                    isCanBeginMatch = true;
                    Environment.Exit(0);
                }
            }

            private async void PrintAlertMessages(RCON rcon)
            {
                if (!isMix)
                {
                    await RconHelper.SendMessage(rcon, $"Basic commands: !ko3 !lo3 !bo1 !bo3 !cm !pause !pause5", Colors.ivory);
                    //await RconHelper.SendMessage(rcon, $"If the statistics stopped working or the match doesn't start, write !rr_node", Colors.legendary);
                    await RconHelper.SendMessage(rcon, $"Note: the match can be started without admin rights", Colors.crimson);
                }
                else
                {
                    if (wannaMixStartDateTime.Subtract(DateTime.Now).TotalSeconds > 0)
                    {
                        await RconHelper.SendMessage(rcon, $"If you are ready write !lo3 to start the mix!", Colors.crimson);
                        await RconHelper.SendMessage(rcon, $"{Math.Round(wannaMixStartDateTime.Subtract(DateTime.Now).TotalMinutes)} minutes remaining!", Colors.crimson);
                    }
                    else
                    {
                        if (!match.IsMatch)
                        {
                            await RconHelper.SendMessage(rcon, $"The mix start failed!", Colors.crimson);
                            await RconHelper.SendCmd(rcon, $"sm_kick @all [kTVCSS] Somebody didn't join the mix! https://ktvcss.ru/mixes");
                            isMix = false;

                            var toBan = AllowedPlayers.Where(x => x.Joined == false);

                            foreach (var player in toBan)
                            {
                                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                                {
                                    await connection.OpenAsync();

                                    SqlCommand query = new SqlCommand("[BanPlayerBySteam]", connection)
                                    {
                                        CommandType = System.Data.CommandType.StoredProcedure
                                    };

                                    query.Parameters.AddWithValue("@STEAMID", player.SteamID);
                                    query.Parameters.AddWithValue("@REASON", "You didn't join the mix!");

                                    await query.ExecuteNonQueryAsync();
                                }
                            }

                            AllowedPlayers.Clear();

                            await MatchEvents.DeleteMix(ServerID);
                        }
                    }
                }
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
                    await RconHelper.SendMessage(rcon, "The bo1/bo3 vote has been cancelled!", Colors.crimson);
                }
            }

            private static void SetAutoRestartTimer()
            {
                aTimer = new System.Timers.Timer(10 * 60 * 1000);
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
                        if (server.ServerType != ServerType.FastCup)
                        {
                            if (OnlinePlayers.Count < match.MinPlayersToStop)
                            {
                                if (match.AScore == 0 && match.BScore == 0 || (match.AScore + match.BScore < 8))
                                {
                                    if (server.ServerType == ServerType.Mix)
                                    {
                                        isMix = false;
                                        AllowedPlayers.Clear();
                                        await MatchEvents.DeleteMix(ServerID);
                                    }
                                    await RconHelper.SendCmd(rcon, "tv_stoprecord");
                                    await MatchEvents.ResetMatch(match.MatchId, server.ID);
                                    await RconHelper.SendMessage(rcon, "The match has been canceled!", Colors.crimson);
                                    await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                                    await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                                    isCanBeginMatch = true;
                                    match = new Match(0, server.ServerType);
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
                                    await RconHelper.SendMessage(rcon, $"Auto-change to the map {mapQueue.FirstOrDefault().Trim()}", Colors.legendary);
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
                    }
                    Thread.Sleep(90000);
                }
            }
        }

        static async Task Main(string[] args)
        {
            Console.Title = "kTVCSS NODE LAUNCHER";
            Console.ForegroundColor = ConsoleColor.Green;
            Logger.Print(0, "Welcome, " + Environment.UserName, LogLevel.Info);

            AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs args)
            {
                Trace.WriteLine("Global exception: " + args.ExceptionObject.ToString());
                try
                {
                    Task.Factory.StartNew(async () =>
                    {
                        using (VkApi api = new VkApi())
                        {
                            await api.AuthorizeAsync(new ApiAuthParams
                            {
                                AccessToken = Program.ConfigTools.Config.VKGroupToken,
                            });

                            await api.Messages.SendAsync(new MessagesSendParams()
                            {
                                ChatId = 5,
                                Message = "Global exception: " + args.ExceptionObject.ToString(),
                                RandomId = new Random().Next()
                            });
                        }
                    });
                }
                catch (Exception ex)
                {
                    Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Debug);
                }
            };

#if DEBUG

            args = new string[1];
            args[0] = "3";

            //string testMessage = CurrentLocale.Values["TestMessage"];
#endif

            if (args.Length != 0)
            {
                Loader.LoadServers();

                ForbiddenWords.AddRange(System.IO.File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "wordsfilter.txt"), System.Text.Encoding.UTF8));
                int id = int.Parse(args[0]);
                id -= 1;
                Console.Title = "[#" + ++id + "]" + $" kTVCSS ({moduleVersion}) @ " + Servers[id].Host + ":" + Servers[id].GamePort;
                Node node = new Node();
                Task.Run(async () => await node.StartNode(Servers[id])).GetAwaiter().GetResult();
            }
        }
    }
}
