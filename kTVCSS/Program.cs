﻿using CoreRCON;
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
            public static List<Player> OnlinePlayers = new List<Player>();
            public List<Player> MatchPlayers = null;
            private bool isCanBeginMatch = true;
            private string tName = "TERRORIST";
            private string ctName = "CT";
            private Match match = new Match(0);
            private bool isResetFreezeTime = false;
            private bool isBestOfOneStarted = false;
            private bool isBestOfThree = false;
            private string currentMapSelector = string.Empty;
            private string terPlayerSelector = string.Empty;
            private string ctPlayerSelector = string.Empty;
            private Dictionary<int, string> mapPool = new Dictionary<int, string>();
            private bool knifeRound = false;
            private RCON rcon = null;
            private string demoName = string.Empty;
            private List<string> mapQueue = new List<string>();
            private string currentMapName = string.Empty;
            public int serverID = 0;
            public int MinPlayersToStart = 8; // 8
            public int MinPlayersToStop = 6; // 6

            public async Task StartNode(Server server)
            {
                Logger.LoggerID = server.ID;

#if DEBUG

                MinPlayersToStart = 0;
                MinPlayersToStop = 0;

#endif
                serverID = server.ID;
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
                await RconHelper.SendMessage(rcon, "Connection to kTVCSS host is established!");
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
                        match.PlayerKills.Clear();
                        match.OpenFragSteamID = string.Empty;

                        if (isResetFreezeTime)
                        {
                            match.IsNeedSetTeamScores = true;
                            await RconHelper.LiveOnThree(rcon, match);
                            isResetFreezeTime = !isResetFreezeTime;
                            if (match.IsOvertime)
                            {
                                await RconHelper.SendCmd(rcon, "mp_startmoney 10000");
                            }
                        }

                        if (match.Pause)
                        {
                            await RconHelper.SendCmd(rcon, "mp_freezetime 10");
                            match.Pause = !match.Pause;
                        }

                        await MatchEvents.InsertMatchLog(match.MatchId, $"<Round Start>", info.Map, server.ID);
                    }
                });

                log.Listen<RestartRound>(result =>
                {
                    
                });

                log.Listen<RoundEndScore>(async result =>
                {
                    if (match.IsMatch)
                    {
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
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a triple kill!");
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a triple kill!", info.Map, server.ID);
                                            break;
                                        }
                                    case 4:
                                        {
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a quad kill!");
                                            await MatchEvents.InsertMatchLog(match.MatchId, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} made a quad kill!", info.Map, server.ID);
                                            break;
                                        }
                                    case 5:
                                        {
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} MADE A RAMPAGE!!!");
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
                            await RconHelper.SendMessage(rcon, "Half time!");
                            await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                            Thread.Sleep(2000);
                            await RconHelper.SendCmd(rcon, "sm_swap @all");
                            await RconHelper.SendMessage(rcon, "One minute timeout!");
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
                                    await RconHelper.SendMessage(rcon, $"Autochanging to {mapQueue.FirstOrDefault().Trim()} in 1 minute!");
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
                                                await RconHelper.SendMessage(rcon, $"Autochanging to {mapQueue.FirstOrDefault().Trim()} in 1 minute!");
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
                                            await RconHelper.SendMessage(rcon, "Overtime!!!");
                                            await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                                            Thread.Sleep(2000);
                                            await RconHelper.SendMessage(rcon, "One minute timeout!");
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
                                    await RconHelper.SendMessage(rcon, $"Autochanging to {mapQueue.FirstOrDefault().Trim()} in 1 minute!");
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
                    if (knifeRound)
                    {
                        int side = 0;
                        if (result.WinningTeam == tName)
                        {
                            side = 2;
                        }
                        else side = 3;
                        await RconHelper.SendCmd(rcon, "exec ktvcss/on_knives_end.cfg");
                        await RconHelper.SendCmd(rcon, $"ChangeVote {side}");
                        knifeRound = false;
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
                            await RconHelper.SendMessage(rcon, $"There are no maps to pick or ban!");
                            return;
                        }
                        if (!mapPool.ContainsKey(mapNum))
                        {
                            await RconHelper.SendMessage(rcon, $"You've been selected the map that already is used!");
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
                                await RconHelper.SendMessage(rcon, $"Selected map: {mapPool.FirstOrDefault().Value.Trim()}");
                                await RconHelper.SendMessage(rcon, $"Autochanging to {mapPool.FirstOrDefault().Value.Trim()} in 5 seconds");
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
                                await RconHelper.SendMessage(rcon, $"The last map is: {mapPool.FirstOrDefault().Value.Trim()}");
                                await RconHelper.SendMessage(rcon, $"Autochanging to {mapQueue.FirstOrDefault().Trim()} in 5 seconds");
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
                                await RconHelper.SendCmd(rcon, $"sm_csay {currentMapSelector} TURN TO BAN!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, please choose the number of the map to ban");
                            }
                        }

                        if (isBestOfThree)
                        {
                            if (mapPool.Count() == 6 || mapPool.Count() == 7)
                            {
                                await RconHelper.SendCmd(rcon, $"sm_csay {currentMapSelector} TURN TO PICK!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, please choose the number of the map to pick");
                            }
                            else
                            {
                                await RconHelper.SendCmd(rcon, $"sm_csay {currentMapSelector} TURN TO BAN!");
                                await RconHelper.SendMessage(rcon, $"{currentMapSelector}, please choose the number of the map to ban");
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
                            await RconHelper.SendMessage(rcon, "The next round will be paused for 1 minute!");
                            await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                            match.TacticalPauses--;
                            match.Pause = true;
                            await RconHelper.SendMessage(rcon, $"You have {match.TacticalPauses} tactical pauses left!");
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "You can no longer pause the game!");
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!pause5" && match.IsMatch)
                    {
                        if (match.TechnicalPauses != 0)
                        {
                            await RconHelper.SendMessage(rcon, "The next round will be paused for 5 minutes!");
                            await RconHelper.SendCmd(rcon, "mp_freezetime 300");
                            match.TechnicalPauses--;
                            match.Pause = true;
                            await RconHelper.SendMessage(rcon, $"You have {match.TechnicalPauses} technical pauses left!");
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "You can no longer pause the game!");
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!bo3") && !isBestOfThree)
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

                        await RconHelper.SendCmd(rcon, $"sm_csay {currentMapSelector} TURN TO BAN!");
                        await RconHelper.SendMessage(rcon, $"The first one - {currentMapSelector}");
                        await RconHelper.SendMessage(rcon, $"Please choose the number of the map to ban:");
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!bo1") && !isBestOfOneStarted)
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

                        await RconHelper.SendCmd(rcon, $"sm_csay {currentMapSelector} TURN TO BAN!");
                        await RconHelper.SendMessage(rcon, $"The first one - {currentMapSelector}");
                        await RconHelper.SendMessage(rcon, $"Please choose the number of the map to ban:");
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!ko3") && isCanBeginMatch)
                    {
                        if (OnlinePlayers.Count < MinPlayersToStart)
                        {
                            await RconHelper.SendCmd(rcon, "sm_msay The match can not be started until the players count less than 8");
                            return;
                        }
                        if (!match.IsMatch)
                        {
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_knives_start.cfg");
                            await RconHelper.Knives(rcon);
                            knifeRound = true;
                        }
                        else
                        {
                            if (match.IsMatch)
                            {
                                await RconHelper.SendMessage(rcon, "The match is already started!");
                            }
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!lo3") && isCanBeginMatch)
                    {
                        if (OnlinePlayers.Count < MinPlayersToStart)
                        {
                            await RconHelper.SendCmd(rcon, "sm_msay The match can not be started until the players count less than 8");
                            return;
                        }
                        if (!match.IsMatch)
                        {
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_start.cfg");
                            SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                            currentMapName = info.Map;
                            demoName = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss") + "_" + info.Map;
                            await RconHelper.SendCmd(rcon, "tv_record " + demoName);
                            await RconHelper.LiveOnThree(rcon, match);
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
                            await RconHelper.SendMessage(rcon, "The match is already started!");
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
                            await RconHelper.SendMessage(rcon, "There are no matches found for recovery!");
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
                        await RconHelper.SendMessage(rcon, "!ko3 - starts a knife round");
                        await RconHelper.SendMessage(rcon, "!lo3 - starts a new match");
                        await RconHelper.SendMessage(rcon, "!bo1 - executes a vote for playing bo1 match");
                        await RconHelper.SendMessage(rcon, "!bo3 - executes a vote for playing bo3 match");
                        await RconHelper.SendMessage(rcon, "!nl - stops a current match half");
                        await RconHelper.SendMessage(rcon, "!score - prints a match score");
                        await RconHelper.SendMessage(rcon, "!cm - force ends a current match");
                        await RconHelper.SendMessage(rcon, "!me - print your stats");
                        await RconHelper.SendMessage(rcon, "!recover - recovers a match due to server crash or etc. Dont use it now");
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message == "!check")
                    {
                        await RconHelper.SendCmd(rcon, "sm_vote \"Are you ready ?\"");
                    }

                    string[] chatWords = chat.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (string word in chatWords)
                    {
                        if (ForbiddenWords.Contains(word.ToLower()))
                        {
                            await RconHelper.SendCmd(rcon, $"sm_gag \"{chat.Player.Name}\" 10 You are muted due to using forbidden words ({word})");
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
                                await RconHelper.SendCmd(rcon, $"sm_kick #{connection.Player.ClientId} Your nickname contains forbidden word ({word})");
                                break;
                            }
                        }

                        Logger.Print(server.ID, $"{connection.Player.Name} ({connection.Player.SteamId}) has been connected to {endpoint.Address}:{endpoint.Port}", LogLevel.Trace);
                    }
                });

                log.Listen<ChangeSides>(async result =>
                {
                    await RconHelper.SendCmd(rcon, "sm_swap @all");
                    await RconHelper.SendMessage(rcon, "Type !lo3 to start a match!");
                });

                log.Listen<NoChangeSides>(async result =>
                {
                    await RconHelper.SendMessage(rcon, "Type !lo3 to start a match!");
                });

                log.Listen<NameChange>(async result =>
                {
                    foreach (string word in ForbiddenWords)
                    {
                        if (result.NewName.ToLower().Contains(word))
                        {
                            await RconHelper.SendCmd(rcon, $"sm_kick #{result.Player.ClientId} Your nickname contains forbidden word ({word})");
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

                log.Listen<NotLive>(async result =>
                {
                    await RconHelper.SendMessage(rcon, "!nl is temporarily disabled");
                    return;
                    if (!match.IsMatch) return;

                    if (match.AScore + match.BScore == match.MaxRounds || (match.AScore == 0 && match.BScore == 0))
                    {
                        match.IsMatch = false;
                        await RconHelper.SendMessage(rcon, "The current match half is reseted!");
                        await RconHelper.SendCmd(rcon, "mp_restartgame 1");
                    }
                    else
                    {
                        await RconHelper.SendMessage(rcon, "You're not allowed to reset match half!");
                    }
                });

                log.Listen<CancelMatch>(async result =>
                {
                    if (match.IsMatch)
                    {
                        if (match.AScore == 0 && match.BScore == 0)
                        {
                            await RconHelper.SendCmd(rcon, "tv_stoprecord");
                            await MatchEvents.ResetMatch(match.MatchId);
                            await RconHelper.SendMessage(rcon, "The match is canceled!");
                            await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                            await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                            isCanBeginMatch = true;
                            match.IsMatch = false;
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
                            await RconHelper.SendMessage(rcon, $"Autochanging to {mapQueue.FirstOrDefault().Trim()} in 1 minute!");
                            Thread.Sleep(60000);
                            await RconHelper.SendCmd(rcon, $"changelevel {mapQueue.FirstOrDefault().Trim()}");
                            mapQueue.Remove(mapQueue.FirstOrDefault());
                        }
                        else if (mapQueue.Count == 0)
                        {
                            isBestOfThree = false;
                        }
                    }
                    if (knifeRound)
                    {
                        await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                        await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                        knifeRound = !knifeRound;
                    }
                });

                log.Listen<PlayerDisconnected>(async connection =>
                {
                    if (connection.Player.SteamId != "STEAM_ID_PENDING" && connection.Player.SteamId != "BOT")
                    {
                        if (match.IsMatch)
                        {
                            Thread banThread = new Thread(BanOnLeftTheMatch)
                            {
                                IsBackground = true
                            };
                            banThread.Start(connection.Player);
                        }
                        else
                        {
                            OnlinePlayers.RemoveAll(x => x.SteamId == connection.Player.SteamId);
                        }
                        Logger.Print(server.ID, $"{connection.Player.Name} ({connection.Player.SteamId}) has been disconnected from {endpoint.Address}:{endpoint.Port}", LogLevel.Trace);
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
                    Logger.Print(serverID, ex.Message, LogLevel.Error);
                }
            }

            private async void BanOnLeftTheMatch(object _player)
            {
                Player player = (Player)_player;
                int previousPlayersCount = OnlinePlayers.Count;
                OnlinePlayers.RemoveAll(x => x.SteamId == player.SteamId);
                Thread.Sleep(3 * 60 * 1000);
                if (!OnlinePlayers.Where(x => x.SteamId == player.SteamId).Any())
                {
                    if (match.IsMatch && previousPlayersCount != OnlinePlayers.Count)
                    {
                        await RconHelper.SendMessage(rcon, $"{player.Name} was banned for left the match!");
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

                await RconHelper.SendMessage(rcon, "The match is over!");
                await RconHelper.SendMessage(rcon, $"Congratulations to the team {tags[winningTeam]}!");
                await RconHelper.SendMessage(rcon, $"{tags[looser]}, good luck next time.");
                await RconHelper.SendMessage(rcon, "Thanks to everyone, cya!");
                await MatchEvents.InsertMatchLog(match.MatchId, $"<Match End>", info.Map, server.ID);
                await MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], info.Map, server.ID, MatchPlayers, winningTeam, match);
                await MatchEvents.InsertDemoName(match.MatchId, demoName);
                isCanBeginMatch = true;
                match.IsMatch = false;
                match.FirstHalf = false;
                Thread.Sleep(3000);
                await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                await RconHelper.SendCmd(rcon, "tv_stoprecord");
            }

            private async Task OnEndMatch(Dictionary<string, string> tags, string winningTeam, string mapName, Server server)
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

                await RconHelper.SendMessage(rcon, "The match is over!");
                await RconHelper.SendMessage(rcon, $"Congratulations to the team {tags[winningTeam]}!");
                await RconHelper.SendMessage(rcon, $"{tags[looser]}, good luck next time.");
                await RconHelper.SendMessage(rcon, "Thanks to everyone, cya!");
                await MatchEvents.InsertMatchLog(match.MatchId, $"<Match End>", mapName, server.ID);
                await MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], mapName, server.ID, MatchPlayers, winningTeam, match);
                await MatchEvents.InsertDemoName(match.MatchId, demoName);
                isCanBeginMatch = true;
                match.IsMatch = false;
                match.FirstHalf = false;
                Thread.Sleep(3000);
                await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                await RconHelper.SendCmd(rcon, "tv_stoprecord");
            }

            private void Rcon_OnDisconnected()
            {
                Logger.Print(serverID, "RCON connection is closed", LogLevel.Warn);
                isCanBeginMatch = true;
                match.IsMatch = false;
            }

            private async void PrintAlertMessages(RCON rcon)
            {
                await RconHelper.SendMessage(rcon, "Basic commands: !ko3 !lo3 !bo1 !bo3 !me !cm !pause !pause5 !check");
                await RconHelper.SendMessage(rcon, "Type !gethelp for getting all cmds available for you");
            }

            private async void CheckIsDeadMapVote()
            {
                Thread.Sleep(120 * 1000);
                if (isBestOfOneStarted)
                {
                    isBestOfOneStarted = !isBestOfOneStarted;
                    await RconHelper.SendMessage(rcon, "The vote for map ban/pick is reset");
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
                        if (OnlinePlayers.Count < MinPlayersToStop)
                        {
                            if (match.AScore == 0 && match.BScore == 0)
                            {
                                await RconHelper.SendCmd(rcon, "tv_stoprecord");
                                await MatchEvents.ResetMatch(match.MatchId);
                                await RconHelper.SendMessage(rcon, "The match is canceled!");
                                await RconHelper.SendCmd(rcon, "exec ktvcss/on_match_end.cfg");
                                await RconHelper.SendCmd(rcon, "exec ktvcss/ruleset_warmup.cfg");
                                isCanBeginMatch = true;
                                match.IsMatch = false;
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
                                await RconHelper.SendMessage(rcon, $"Autochanging to {mapQueue.FirstOrDefault().Trim()} in 1 minute!");
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

            #if DEBUG

            args = new string[1];
            args[0] = "0";

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
            else
            {
                Logger.Print(0, "Attempt to load servers from database", LogLevel.Info);
                Loader.LoadServers();
                Logger.Print(0, "Loaded " + Servers.Count + " servers", LogLevel.Info);
                foreach (var server in Servers)
                {
                    Process node = new Process();
                    node.StartInfo.UseShellExecute = true;
                    node.StartInfo.FileName = "kTVCSS.exe";
                    node.StartInfo.Arguments = (server.ID - 1).ToString();
                    node.Start();
                }
            }
        }
    }
}
