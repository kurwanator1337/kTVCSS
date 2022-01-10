using CoreRCON;
using CoreRCON.PacketFormats;
using CoreRCON.Parsers.Standard;
using kTVCSS.Servers;
using kTVCSS.Settings;
using kTVCSS.Tools;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Data.SqlClient;

namespace kTVCSS
{
    class Program
    {
        public static Logger Logger = new Logger();
        public static ConfigTools ConfigTools = new ConfigTools();
        public static List<Server> Servers = new List<Server>();

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
            private Match match = null;
            private bool isResetFreezeTime = false;
            private bool isBestOfOneStarted = false;
            private string currentMapSelector = string.Empty;
            private string terPlayerSelector = string.Empty;
            private string ctPlayerSelector = string.Empty;
            private Dictionary<int, string> mapPool = new Dictionary<int, string>();
            private bool knifeRound = false;
            private RCON rcon = null;
            private string demoName = string.Empty;

            public async Task StartNode(Server server)
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(server.Host), server.GamePort);
                rcon = new RCON(endpoint, server.RconPassword);
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
                        Logger.Print($"[#{server.ID}] {ex.Message}", LogLevel.Error);
                        Thread.Sleep(30000);
                    }
                }
                LogReceiver log = new LogReceiver(server.NodePort, endpoint);
                ServerQueryPlayer[] players = await ServerQuery.Players(endpoint);
                SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                Logger.Print($"Created connection to {info.Name}", LogLevel.Trace);
                await RconHelper.SendMessage(rcon, "Соединение до сервера kTVCSS было успешно установлено");
                alertThread.Start(server);

                log.Listen<KillFeed>(async kill =>
                {
                    if (match is not null && match.IsMatch)
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
                    }
                });

                log.Listen<RoundStart>(async result =>
                {
                    if (match is not null && match.IsMatch)
                    {
                        match.PlayerKills.Clear();
                        match.OpenFragSteamID = string.Empty;

                        if (isResetFreezeTime)
                        {
                            await RconHelper.SendCmd(rcon, "zb_lo3");
                            isResetFreezeTime = !isResetFreezeTime;
                            if (match.IsOvertime)
                            {
                                await RconHelper.SendCmd(rcon, "mp_startmoney 10000");
                            }
                        }
                    }
                });

                log.Listen<RoundEndScore>(async result =>
                {
                    if (match is not null && match.IsMatch)
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

                        await RconHelper.SendMessage(rcon, $"Счет матча: {tags[tName]} [{match.AScore}-{match.BScore}] {tags[ctName]}");
                        if (match.AScore + match.BScore == match.MaxRounds || (match.AScoreOvertime + match.BScoreOvertime == match.MaxRounds && match.AScoreOvertime != match.BScoreOvertime))
                        {
                            await RconHelper.SendMessage(rcon, "Половина матча сыграна! Смена сторон!");
                            await RconHelper.SendCmd(rcon, "mp_freezetime 60");
                            Thread.Sleep(2000);
                            await RconHelper.SendCmd(rcon, "sm_swap @all");
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
                                string looser = string.Empty;
                                if (result.WinningTeam == tName)
                                {
                                    looser = ctName;
                                }
                                else
                                {
                                    looser = tName;
                                }
                                // score check
                                await RconHelper.SendCmd(rcon, "tv_stoprecord");
                                await RconHelper.SendMessage(rcon, "Матч сыгран!");
                                await RconHelper.SendMessage(rcon, $"Поздравляем команду {tags[result.WinningTeam]} с победой!");
                                await RconHelper.SendMessage(rcon, $"{tags[looser]}, в следующий раз вам повезет.");
                                await RconHelper.SendMessage(rcon, "Спасибо за игру, надеюсь, увидимся скоро!");

                                await MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], info.Map, server.ID, MatchPlayers, result.WinningTeam, match);
                                isCanBeginMatch = true;
                                match.IsMatch = false;
                            }
                            else
                            {
                                if (match.AScore + match.BScore == match.MaxRounds * 2 || match.AScoreOvertime + match.BScoreOvertime == match.MaxRounds * 2)
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

                        match.RoundID++;

                        foreach (var player in match.PlayerKills)
                        {
                            if (player.Value >= 3)
                            {
                                await MatchEvents.SetHighlight(player.Key, player.Value);
                                switch (player.Value)
                                {
                                    case 3:
                                        {
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} TRIPPLE KILL!");
                                            break;
                                        }
                                    case 4:
                                        {
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} QUADRO KILL!");
                                            break;
                                        }
                                    case 5:
                                        {
                                            await RconHelper.SendMessage(rcon, $"{MatchPlayers.Find(x => x.SteamId == player.Key).Name} RAMPAGE!!!");
                                            break;
                                        }
                                }
                            }
                        }
                        await MatchEvents.SetOpenFrag(match.OpenFragSteamID);
                        await MatchEvents.UpdateMatchScore(match.AScore, match.BScore, server.ID, match.MatchId);
                        if (!match.FirstHalf)
                        {
                            if ((Math.Abs(match.AScore - match.BScore) >= 2) && (match.AScore == match.MaxRounds + 1 || match.BScore == match.MaxRounds + 1))
                            {
                                string looser = string.Empty;
                                if (result.WinningTeam == tName)
                                {
                                    looser = ctName;
                                }
                                else
                                {
                                    looser = tName;
                                }

                                await RconHelper.SendCmd(rcon, "tv_stoprecord");
                                await RconHelper.SendMessage(rcon, "Матч сыгран!");
                                await RconHelper.SendMessage(rcon, $"Поздравляем команду {tags[result.WinningTeam]} с победой!");
                                await RconHelper.SendMessage(rcon, $"{tags[looser]}, в следующий раз вам повезет.");
                                await RconHelper.SendMessage(rcon, "Спасибо за игру, надеюсь, увидимся скоро!");

                                await MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], info.Map, server.ID, MatchPlayers, result.WinningTeam, match);
                                isCanBeginMatch = true;
                                match.IsMatch = false;
                            }
                        }
                    }
                    if (knifeRound)
                    {
                        await RconHelper.SendCmd(rcon, "sm_knife_round 0");
                        knifeRound = false;
                    }
                });

                log.Listen<ChatMessage>(async chat =>
                {
                    int.TryParse(chat.Message, out int mapNum);
                    if (chat.Channel == MessageChannel.All && mapNum > 0 && mapNum < 10 && currentMapSelector == chat.Player.Name)
                    {
                        if (!mapPool.ContainsKey(mapNum))
                        {
                            await RconHelper.SendMessage(rcon, $"Вы ввели номер карты, который уже был забанен");
                            return;
                        }

                        mapPool.Remove(mapNum);

                        if (currentMapSelector == ctPlayerSelector)
                        {
                            currentMapSelector = terPlayerSelector;
                        }
                        else
                        {
                            currentMapSelector = ctPlayerSelector;
                        }

                        foreach (var map in mapPool)
                        {
                            await RconHelper.SendMessage(rcon, $"{map.Key} - {map.Value}");
                        }

                        if (mapPool.Count() == 1)
                        {
                            await RconHelper.SendMessage(rcon, $"Выбрана карта: {mapPool.FirstOrDefault().Value}");
                            Thread.Sleep(5000);
                            await RconHelper.SendCmd(rcon, $"changelevel {mapPool.FirstOrDefault().Value}");
                            currentMapSelector = string.Empty;
                            isBestOfOneStarted = false;
                            terPlayerSelector = string.Empty;
                            ctPlayerSelector = string.Empty;
                            mapPool.Clear();
                        }

                        await RconHelper.SendMessage(rcon, $"{currentMapSelector}, напишите номер карты для бана");
                    }   
                    
                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!bo1") && !isBestOfOneStarted)
                    {
                        isBestOfOneStarted = true;
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

                        var tempPlayers = result.Split('\n');
                        terPlayerSelector = tempPlayers.First(x => x.Contains(";2")).Split(';')[0];
                        ctPlayerSelector = tempPlayers.First(x => x.Contains(";3")).Split(';')[0];

                        await RconHelper.SendMessage(rcon, "Список карт для черка:");

                        foreach (var map in mapPool)
                        {
                            await RconHelper.SendMessage(rcon, $"{map.Key} - {map.Value}");
                        }

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

                        await RconHelper.SendMessage(rcon, $"Первым начинает черк - {currentMapSelector}");
                        await RconHelper.SendMessage(rcon, $"Напишите номер карты, чтобы забанить её: ");
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!ko3") && isCanBeginMatch)
                    {
                        if (match is null)
                        {
                            await RconHelper.SendCmd(rcon, "zb_ko3");
                            await RconHelper.SendCmd(rcon, "sm_knife_round 1");
                            knifeRound = true;
                        }
                        else
                        {
                            if (match.IsMatch)
                            {
                                await RconHelper.SendMessage(rcon, "Матч уже запущен");
                            }
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!lo3") && isCanBeginMatch)
                    {
                        if (match is null)
                        {
                            await RconHelper.SendCmd(rcon, "zb_lo3");
                            SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                            demoName = DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss") + "_" + info.Map;
                            await RconHelper.SendCmd(rcon, "tv_record " + demoName);
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
                            if (match.IsMatch)
                            {
                                await RconHelper.SendMessage(rcon, "Матч уже запущен");
                            }
                            else
                            {
                                await RconHelper.SendMessage(rcon, "LIVE");
                                match.IsMatch = true;
                            }
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
                            await RconHelper.SendMessage(rcon, $"Счет матча: {tName} [{match.AScore}-{match.BScore}] {ctName}");
                            foreach (var player in MatchPlayers)
                            {
                                await OnPlayerConnectAuth.AuthPlayer(player.SteamId, player.Name);
                            }
                        }
                        else
                        {
                            await RconHelper.SendMessage(rcon, "Не найдено матчей для восстановления");
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!nl") && match.IsMatch)
                    {
                        if (match is not null)
                        {
                            if (match.AScore + match.BScore == match.MaxRounds || (match.AScore == 0 && match.BScore == 0))
                            {
                                match.IsMatch = false;
                                await RconHelper.SendMessage(rcon, "Текущая половина матча сброшена!");
                                await RconHelper.SendCmd(rcon, "mp_restartgame 1");
                            }
                            else
                            {
                                await RconHelper.SendMessage(rcon, "Текущую половину уже нельзя сбросить!");
                            }
                        }
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!score") && match is not null)
                    {
                        await RconHelper.SendMessage(rcon, $"Счет матча: {tName} [{match.AScore}-{match.BScore}] {ctName}");
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!cm") && match.IsMatch)
                    {
                        if (match is not null)
                        {
                            if (match.AScore == 0 && match.BScore == 0)
                            {
                                await RconHelper.SendCmd(rcon, "tv_stoprecord");
                                await MatchEvents.ResetMatch(match.MatchId);
                                await RconHelper.SendMessage(rcon, "Матч сброшен!");
                                match = null;
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

                            await RconHelper.SendCmd(rcon, "tv_stoprecord");
                            await RconHelper.SendMessage(rcon, "Матч сыгран!");
                            await RconHelper.SendMessage(rcon, $"Поздравляем команду {tags[winner]} с победой!");
                            await RconHelper.SendMessage(rcon, $"{tags[looser]}, в следующий раз вам повезет.");
                            await RconHelper.SendMessage(rcon, "Спасибо за игру, надеюсь, увидимся скоро!");
                            
                            await MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], info.Map, server.ID, MatchPlayers, winner, match);
                            isCanBeginMatch = true;
                            match.IsMatch = false;
                        }
                    }
                });

                log.Listen<PlayerConnected>(async connection =>
                {
                    if (connection.Player.SteamId != "STEAM_ID_PENDING" && connection.Player.SteamId != "BOT")
                    {
                        var result = await OnPlayerConnectAuth.AuthPlayer(connection.Player.SteamId, connection.Player.Name);
                        OnlinePlayers.Add(connection.Player);
                        if (match is not null && match.IsMatch)
                        {
                            if (!MatchPlayers.Where(x => x.SteamId == connection.Player.SteamId).Any()) MatchPlayers.Add(connection.Player);
                        }
                        Logger.Print($"{connection.Player.Name} ({connection.Player.SteamId}) has been connected to {endpoint.Address}:{endpoint.Port}", LogLevel.Trace);
                    }
                });

                log.Listen<PlayerDisconnected>(connection =>
                {
                    if (connection.Player.SteamId != "STEAM_ID_PENDING" && connection.Player.SteamId != "BOT")
                    {
                        OnlinePlayers.RemoveAll(x => x.SteamId == connection.Player.SteamId);
                        // add ban for leaving the match if its mix and not a clan match
                        Logger.Print($"{connection.Player.Name} ({connection.Player.SteamId}) has been disconnected from {endpoint.Address}:{endpoint.Port}", LogLevel.Trace);
                    }
                });

                await Task.Delay(-1);
            }

            private async void PrintAlertMessages(RCON rcon)
            {
                await RconHelper.SendMessage(rcon, "Доступные команды: !ko3, !lo3, !bo1, !nl, !cm, !recover");
            }

            private void Alerter(object _server)
            {
                Thread.Sleep(30000);
                while (true)
                {
                    if (match is not null && !match.IsMatch)
                    {
                        PrintAlertMessages(rcon);
                    }
                    Thread.Sleep(60000);
                }
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "kTVCSS PLAYER STATISTICS PROCESSOR MATCH CONTROLLER AND MORE";
            Console.ForegroundColor = ConsoleColor.Green;

            Logger.Print("Welcome, " + Environment.UserName, LogLevel.Info);
            Logger.Print("Attempt to load servers from database", LogLevel.Info);
            Loader.LoadServers();
            Logger.Print("Loaded " + Servers.Count + " servers", LogLevel.Info);
            foreach (var server in Servers)
            {
                Node node = new Node();
                Task.Run(async () => await node.StartNode(server)).GetAwaiter().GetResult();
            }
        }
    }
}
