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

namespace kTVCSS
{
    class Program
    {
        public static Logger Logger = new Logger();
        public static ConfigTools ConfigTools = new ConfigTools();
        public static List<Server> Servers = new List<Server>();

        public class Node
        {
            public static List<Player> OnlinePlayers = new List<Player>();
            public List<Player> MatchPlayers = null;
            private bool isCanBeginMatch = true;
            private string tName = "TERRORIST";
            private string ctName = "CT";
            Match match = null;

            public async Task StartNode(Server server)
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(server.Host), server.GamePort);
                RCON rcon = new RCON(endpoint, server.RconPassword);
                //await rcon.ConnectAsync();
                LogReceiver log = new LogReceiver(server.NodePort, endpoint);
                ServerQueryPlayer[] players = await ServerQuery.Players(endpoint);
                SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                Logger.Print($"Created connection to {info.Name}", LogLevel.Trace);

                log.Listen<KillFeed>(async kill =>
                {
                    if (match is not null)
                    {
                        if (match.IsMatch)
                        {
                            int hs = 0;
                            if (kill.Headshot)
                                hs = 1;
                            await OnPlayerKill.SetValues(kill.Killer.SteamId, kill.Killed.SteamId, hs, server.ID, match.MatchId);
                            if (MatchPlayers.Where(x => x.SteamId == kill.Killer.SteamId).Count() == 0) MatchPlayers.Add(kill.Killer);
                            if (MatchPlayers.Where(x => x.SteamId == kill.Killed.SteamId).Count() == 0) MatchPlayers.Add(kill.Killed);
                        }
                    }  
                });

                log.Listen<RoundEndScore>(async result =>
                {
                    if (match is not null)
                    {
                        if (match.IsMatch)
                        {
                            if (result.WinningTeam == tName)
                            {
                                match.AScore += 1;
                            }
                            if (result.WinningTeam == ctName)
                            {
                                match.BScore += 1;
                            }
                            if (match.AScore + match.BScore == match.MaxRounds)
                            {
                                match.FirstHalf = false;
                                var _aScore = match.AScore;
                                var _bScore = match.BScore;
                                match.AScore = _bScore;
                                match.BScore = _aScore;
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
                            await MatchEvents.UpdateMatchScore(match.AScore, match.BScore, server.ID, match.MatchId);
                            if (!match.FirstHalf)
                            {
                                if ((Math.Abs(match.AScore - match.BScore) >= 2) && (match.AScore == match.MaxRounds + 1 || match.BScore == match.MaxRounds + 1))
                                {
                                    SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                                    var tags = MatchEvents.GetTeamNames(MatchPlayers);
                                    await MatchEvents.FinishMatch(match.AScore, match.BScore, tags[tName], tags[ctName], info.Map, server.ID, MatchPlayers, result.WinningTeam, match);
                                    isCanBeginMatch = true;
                                    match.IsMatch = false;
                                }
                            }
                        }
                    }
                });

                log.Listen<ChatMessage>(async chat =>
                {
                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!lo3") && isCanBeginMatch)
                    {
                        SourceQueryInfo info = await ServerQuery.Info(endpoint, ServerQuery.ServerType.Source) as SourceQueryInfo;
                        match = new Match(3);
                        match.MatchId = await MatchEvents.CreateMatch(server.ID, info.Map);
                        MatchPlayers = new List<Player>();
                        MatchPlayers = OnlinePlayers;
                    }

                    if (chat.Channel == MessageChannel.All && chat.Message.StartsWith("!recover") && isCanBeginMatch)
                    {
                        var recoveredMatchID = await MatchEvents.CheckMatchLiveExists(server.ID);
                        if (recoveredMatchID != 0)
                        {
                            MatchPlayers = new List<Player>();
                            MatchPlayers = OnlinePlayers;
                            match = new Match(3);
                            match.MatchId = recoveredMatchID;
                            match = await MatchEvents.GetLiveMatchResults(server.ID, match);
                            if (match.AScore + match.BScore >= 15)
                            {
                                match.FirstHalf = false;
                            }
                        }
                    }
                });

                log.Listen<PlayerConnected>(async connection =>
                {
                    if (connection.Player.SteamId != "STEAM_ID_PENDING" && connection.Player.SteamId != "BOT")
                    {
                        var result = await OnPlayerConnectAuth.AuthPlayer(connection.Player.SteamId, connection.Player.Name);
                        OnlinePlayers.Add(connection.Player);
                        if (match is not null)
                        {
                            if (match.IsMatch)
                            {
                                MatchPlayers.Add(connection.Player);
                            }  
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
