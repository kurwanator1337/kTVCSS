using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace PrintAVGTeamsTest
{
    internal class Program
    {
        public class Player
        {
            public int ClientId { get; set; }
            public string Name { get; set; }
            public string SteamId { get; set; }
            public string Team { get; set; }
        }

        public class PlayerRank
        {
            public string SteamID { get; set; }
            public int Points { get; set; }
        }

        public static List<Player> MatchPlayers = new List<Player>();
        public static List<PlayerRank> PlayersRank = new List<PlayerRank>();
        private static string tName = "TERRORIST";
        private static string ctName = "CT";

        public static async Task<PlayerRank> GetPlayerRank(string steamID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(""))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT MMR FROM [dbo].[Players] WHERE STEAMID = '{steamID}'", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            PlayerRank playerRank = new PlayerRank();

                            int.TryParse(reader[0].ToString(), out int mmr);

                            playerRank.SteamID = steamID;
                            playerRank.Points = mmr;

                            return playerRank;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                
            }
            return null;
        }

        public static Dictionary<string, string> GetTeamNames(List<Player> players)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>
            {
                { "TERRORIST", "TERRORIST" },
                { "CT", "CT" }
            };

            try
            {
                List<string> ctTags = new List<string>();
                List<string> terTags = new List<string>();

                IEnumerable<Player> ctPlayers = players.Where(x => x.Team == "CT");
                foreach (var player in ctPlayers)
                {
                    ctTags.Add(player.Name.Split(' ')[0]);
                }

                IEnumerable<Player> terPlayers = players.Where(x => x.Team == "TERRORIST");
                foreach (var player in terPlayers)
                {
                    terTags.Add(player.Name.Split(' ')[0]);
                }

                tags["CT"] = "Team " + ctTags[0];
                tags["TERRORIST"] = "Team " + terTags[0];

                foreach (var possibleTag in ctTags)
                {
                    if (ctTags.Count(x => x == possibleTag) >= 2)
                    {
                        tags["CT"] = possibleTag;
                        break;
                    }
                }

                foreach (var possibleTag in terTags)
                {
                    if (terTags.Count(x => x == possibleTag) >= 2)
                    {
                        tags["TERRORIST"] = possibleTag;
                        break;
                    }
                }

                return tags;
            }
            catch (Exception ex)
            {
                
                return tags;
            }
        }

        static async Task Main(string[] args)
        {
            int i = 0;
            using (SqlConnection connection = new SqlConnection(""))
            {
                connection.Open();
                var query = new SqlCommand("select steamid, name from matchesresults where id = 668", connection);
                using (var reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (i < 5)
                        {
                            MatchPlayers.Add(new Player()
                            {
                                SteamId = reader[0].ToString(),
                                Name = reader[1].ToString(),
                                Team = tName
                            });
                        }
                        else
                        {
                            MatchPlayers.Add(new Player()
                            {
                                SteamId = reader[0].ToString(),
                                Name = reader[1].ToString(),
                                Team = ctName
                            });
                        }
                        i++;
                        var playerRank = await GetPlayerRank(reader[0].ToString());
                        PlayersRank.Add(playerRank);
                    }
                }
            }

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
                                var tags = GetTeamNames(MatchPlayers);
                                Console.WriteLine($"Средний рейтинг команды {tags[tName]} - {ter}");
                                Console.WriteLine($"Средний рейтинг команды {tags[ctName]} - {ct}");
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
}
