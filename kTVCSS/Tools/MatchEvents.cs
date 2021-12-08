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

        public static async Task FinishMatch(int AScore, int BScore, string AName, string BName, string Map, int serverId, List<Player> players, string winnerTeam)
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
                // updating players
                foreach (Player player in players)
                {
                    if (player.Team == winnerTeam)
                        await SetPlayerMatchResult(player.SteamId, 1);
                    else await SetPlayerMatchResult(player.SteamId, 0);
                }
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
