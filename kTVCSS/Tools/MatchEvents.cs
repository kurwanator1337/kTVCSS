using CoreRCON.Parsers.Standard;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Tools
{
    public static class MatchEvents
    {
        public static async Task<int> CheckMatchLiveExists(int serverId)
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand("[dbo].[MatchLiveCheckExists]", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                SqlParameter serverParam = new SqlParameter
                {
                    ParameterName = "@SERVERID",
                    Value = serverId
                };
                
                query.Parameters.Add(serverParam);
                var result = await query.ExecuteScalarAsync();
                await connection.CloseAsync();
                return int.Parse(result.ToString());
            }
        }

        public static async Task<int> CreateMatch(int serverId, string mapName)
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand("[dbo].[CreateMatch]", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                SqlParameter serverParam = new SqlParameter
                {
                    ParameterName = "@SERVERID",
                    Value = serverId
                };
                SqlParameter mapParam = new SqlParameter
                {
                    ParameterName = "@MAP",
                    Value = mapName
                };

                query.Parameters.Add(serverParam);
                query.Parameters.Add(mapParam);
                var result = await query.ExecuteScalarAsync();
                await connection.CloseAsync();
                return int.Parse(result.ToString());
            }
        }

        public static async Task UpdateMatchScore(int AScore, int BScore, int serverId, int matchId)
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand("[dbo].[UpdateMatchScore]", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                SqlParameter serverParam = new SqlParameter
                {
                    ParameterName = "@SERVERID",
                    Value = serverId
                };
                SqlParameter aParam = new SqlParameter
                {
                    ParameterName = "@ASCORE",
                    Value = AScore
                };
                SqlParameter bParam = new SqlParameter
                {
                    ParameterName = "@BSCORE",
                    Value = BScore
                };
                SqlParameter mParam = new SqlParameter
                {
                    ParameterName = "@ID",
                    Value = matchId
                };

                query.Parameters.Add(serverParam);
                query.Parameters.Add(aParam);
                query.Parameters.Add(bParam);
                query.Parameters.Add(mParam);
                await query.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
        }

        public static async Task<Match> GetLiveMatchResults(int serverId, Match match)
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand($"SELECT ASCORE, BSCORE FROM [dbo].[MatchesLive] WHERE ID = {match.MatchId} AND SERVERID = {serverId} AND FINISHED = 0", connection);
                using (var reader = await query.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        match.AScore = int.Parse(reader[0].ToString());
                        match.BScore = int.Parse(reader[1].ToString());
                    }
                }
                await connection.CloseAsync();
                return match;
            }
        }

        public static async Task FinishMatch(int AScore, int BScore, string AName, string BName, string Map, int serverId, List<Player> players, string winnerTeam, Match match)
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand("[dbo].[EndMatch]", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                SqlParameter serverParam = new SqlParameter
                {
                    ParameterName = "@SERVERID",
                    Value = serverId
                };
                SqlParameter aParam = new SqlParameter
                {
                    ParameterName = "@ASCORE",
                    Value = AScore
                };
                SqlParameter bParam = new SqlParameter
                {
                    ParameterName = "@BSCORE",
                    Value = BScore
                };
                SqlParameter asParam = new SqlParameter
                {
                    ParameterName = "@ANAME",
                    Value = AName
                };
                SqlParameter bsParam = new SqlParameter
                {
                    ParameterName = "@BNAME",
                    Value = BName
                };
                SqlParameter mapParam = new SqlParameter
                {
                    ParameterName = "@MAP",
                    Value = Map
                };

                query.Parameters.Add(serverParam);
                query.Parameters.Add(aParam);
                query.Parameters.Add(bParam);
                query.Parameters.Add(asParam);
                query.Parameters.Add(bsParam);
                query.Parameters.Add(mapParam);
                await query.ExecuteNonQueryAsync();
                await connection.CloseAsync();

                var teamTags = GetTeamNames(players);

                foreach (Player player in players)
                {
                    if (player.Team == winnerTeam)
                        await SetPlayerMatchResult(player.SteamId, 1);
                    else await SetPlayerMatchResult(player.SteamId, 0);
                    await UpdateMatchesResults(player.SteamId, match.MatchId, teamTags[player.Team], player.Name, serverId);
                }
            }
        }

        public static Dictionary<string, string> GetTeamNames(List<Player> players)
        {
            Dictionary<string, string> tags = new Dictionary<string, string>();
            tags.Add("TERRORIST", "Team Alpha");
            tags.Add("CT", "Team Bravo");
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

            foreach (var possibleTag in ctTags)
            {
                if (ctTags.Count(x => x == possibleTag) >= 3)
                {
                    tags["CT"] = possibleTag;
                    break;
                }
            }

            foreach (var possibleTag in terTags)
            {
                if (terTags.Count(x => x == possibleTag) >= 3)
                {
                    tags["T"] = possibleTag;
                    break;
                }
            }

            return tags;
        }

        private async static Task UpdateMatchesResults(string steamId, int matchId, string teamName, string playerName, int serverId)
        {
            string kills = string.Empty;
            string deaths = string.Empty;
            string headshots = string.Empty;
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand($"SELECT [KILLS], [DEATHS], [HEADSHOTS] FROM [kTVCSS].[dbo].[MatchesResultsLive] WHERE ID = {matchId} AND STEAMID = '{steamId}'", connection);
                using (var reader = await query.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        kills = reader[0].ToString();
                        deaths = reader[1].ToString();
                        headshots = reader[2].ToString();
                    }
                }
                await connection.CloseAsync();
            }
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand($"INSERT INTO [kTVCSS].[dbo].[MatchesResults] (ID, TEAMNAME, NAME, STEAMID, KILLS, DEATHS, HEADSHOTS, SERVERID)" +
                    $" VALUES ({matchId}, '{teamName}', '{playerName}', '{steamId}', {kills}, {deaths}, {headshots}, {serverId})", connection);
                await query.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
        }

        public async static Task SetHighlight(string steamId, int killsCount)
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand("[dbo].[OnPlayerHighlight]", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                SqlParameter killParam = new SqlParameter
                {
                    ParameterName = "@KILLS",
                    Value = killsCount
                };
                SqlParameter steamParam = new SqlParameter
                {
                    ParameterName = "@STEAMID",
                    Value = steamId
                };
                

                query.Parameters.Add(killParam);
                query.Parameters.Add(steamParam);
                await query.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
        }

        public async static Task SetOpenFrag(string steamId)
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand("[dbo].[OnPlayerOpenFrag]", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                SqlParameter steamParam = new SqlParameter
                {
                    ParameterName = "@STEAMID",
                    Value = steamId
                };


                query.Parameters.Add(steamParam);
                await query.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
        }

        private async static Task SetPlayerMatchResult(string steamId, int win)
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand("[dbo].[SetPlayerMatchResult]", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                SqlParameter steamParam = new SqlParameter
                {
                    ParameterName = "@STEAMID",
                    Value = steamId
                };
                SqlParameter winParam = new SqlParameter
                {
                    ParameterName = "@WIN",
                    Value = win
                };
                query.Parameters.Add(steamParam);
                query.Parameters.Add(winParam);
                await query.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
        }
    }
}
