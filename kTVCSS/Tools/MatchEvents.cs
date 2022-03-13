using CoreRCON.Parsers.Standard;
using kTVCSS.Models;
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
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[MatchLiveCheckExists]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    query.Parameters.AddWithValue("@SERVERID", serverId);

                    var result = await query.ExecuteScalarAsync();
                    await connection.CloseAsync();
                    return int.Parse(result.ToString());
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
                return 0;
            }
        }

        public static async Task InsertPlayerRatingProgress(string steamID, int mmr)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[InsertRatingProgress]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    query.Parameters.AddWithValue("@STEAMID", steamID);
                    query.Parameters.AddWithValue("@MMR", mmr);

                    var result = await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }

        public static async Task<int> CreateMatch(int serverId, string mapName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[CreateMatch]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    query.Parameters.AddWithValue("@SERVERID", serverId);
                    query.Parameters.AddWithValue("@MAP", mapName);

                    var result = await query.ExecuteScalarAsync();
                    await connection.CloseAsync();
                    return int.Parse(result.ToString());
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
                return 0;
            }
        }

        public static async Task UpdateMatchScore(int AScore, int BScore, int serverId, int matchId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[UpdateMatchScore]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    query.Parameters.AddWithValue("@SERVERID", serverId);
                    query.Parameters.AddWithValue("@ASCORE", AScore);
                    query.Parameters.AddWithValue("@BSCORE", BScore);
                    query.Parameters.AddWithValue("@ID", matchId);

                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
            }
        }

        public static async Task InsertDemoName(int matchId, string demoName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[InsertDemoName]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    query.Parameters.AddWithValue("@MATCHID", matchId);
                    query.Parameters.AddWithValue("@DEMONAME", demoName);

                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }

        public static async Task<Match> GetLiveMatchResults(int serverId, Match match)
        {
            try
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
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
                return match;
            }
        }

        public static void FinishMatch(int AScore, int BScore, string AName, string BName, string Map, int serverId, List<Player> players, string winnerTeam, Match match)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    connection.Open();
                    SqlCommand query = new SqlCommand("[dbo].[EndMatch]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    query.Parameters.AddWithValue("@SERVERID", serverId);
                    query.Parameters.AddWithValue("@ASCORE", AScore);
                    query.Parameters.AddWithValue("@BSCORE", BScore);
                    query.Parameters.AddWithValue("@ANAME", AName);
                    query.Parameters.AddWithValue("@BNAME", BName);
                    query.Parameters.AddWithValue("@MAP", Map);

                    query.ExecuteNonQuery();
                    connection.Close();

                    var teamTags = GetTeamNames(players);

                    foreach (Player player in players)
                    {
                        if (player.Team == winnerTeam)
                        {
                            int playerPts = Program.Node.PlayersRank.Where(x => x.SteamID == player.SteamId).First().Points;

                            var enemies = players.Where(x => x.Team != winnerTeam);
                            int enemiesSum = 0;
                            int enemiesCount = 0;
                            foreach (var item in enemies)
                            {
                                try
                                {
                                    int pts = Program.Node.PlayersRank.Where(x => x.SteamID == item.SteamId).First().Points;
                                    enemiesSum += pts;
                                    if (pts != 0)
                                    {
                                        enemiesCount++;
                                    }
                                }
                                catch (Exception)
                                {
                                    // null?
                                }
                            }
                            if (enemiesCount != 0)
                            {
                                double enemiesAvg = double.Parse(enemiesSum.ToString()) / double.Parse(enemiesCount.ToString());
                                var diff = playerPts - enemiesAvg;
                                if (diff > 500)
                                {
                                    SetPlayerMatchResult(player.SteamId, 1, 10);
                                }
                                else
                                {
                                    SetPlayerMatchResult(player.SteamId, 1, 25);
                                }
                            }
                            else
                            {
                                SetPlayerMatchResult(player.SteamId, 1, 25);
                            }
                        }   
                        else
                        {
                            int playerPts = Program.Node.PlayersRank.Where(x => x.SteamID == player.SteamId).First().Points;

                            var enemies = players.Where(x => x.Team == winnerTeam);
                            int enemiesSum = 0;
                            int enemiesCount = 0;
                            foreach (var item in enemies)
                            {
                                try
                                {
                                    int pts = Program.Node.PlayersRank.Where(x => x.SteamID == item.SteamId).First().Points;
                                    enemiesSum += pts;
                                    if (pts != 0)
                                    {
                                        enemiesCount++;
                                    }
                                }
                                catch (Exception)
                                {
                                    // null?
                                }
                            }
                            if (enemiesCount != 0)
                            {
                                double enemiesAvg = double.Parse(enemiesSum.ToString()) / double.Parse(enemiesCount.ToString());
                                var diff = playerPts - enemiesAvg;
                                if (diff > 500)
                                {
                                    SetPlayerMatchResult(player.SteamId, 0, -25);
                                }
                                else
                                {
                                    SetPlayerMatchResult(player.SteamId, 0, -10);
                                }
                            }
                            else
                            {
                                SetPlayerMatchResult(player.SteamId, 0, -25);
                            }
                        }

                        UpdateMatchesResults(player.SteamId, match.MatchId, teamTags[player.Team], player.Name, serverId);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
            }
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
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
                return tags;
            }
        }

        private static void UpdateMatchesResults(string steamId, int matchId, string teamName, string playerName, int serverId)
        {
            try
            {
                string kills = string.Empty;
                string deaths = string.Empty;
                string headshots = string.Empty;
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    connection.Open();
                    SqlCommand query = new SqlCommand($"SELECT [KILLS], [DEATHS], [HEADSHOTS] FROM [kTVCSS].[dbo].[MatchesResultsLive] WHERE ID = {matchId} AND STEAMID = '{steamId}'", connection);
                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            kills = reader[0].ToString();
                            deaths = reader[1].ToString();
                            headshots = reader[2].ToString();
                        }
                    }
                    connection.Close();
                }
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    connection.Open();
                    try
                    {
                        SqlCommand query = new SqlCommand("[dbo].[InsertMatchResult]", connection)
                        {
                            CommandType = System.Data.CommandType.StoredProcedure
                        };
                        query.Parameters.AddWithValue("@MATCHID", matchId);
                        query.Parameters.AddWithValue("@TEAMNAME", teamName);
                        query.Parameters.AddWithValue("@PLAYERNAME", playerName);
                        query.Parameters.AddWithValue("@STEAMID", steamId);
                        query.Parameters.AddWithValue("@KILLS", kills);
                        query.Parameters.AddWithValue("@DEATHS", deaths);
                        query.Parameters.AddWithValue("@HEADSHOTS", headshots);
                        query.Parameters.AddWithValue("@SERVERID", serverId);
                        query.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
            }
        }

        public async static Task InsertMatchLog(int matchId, string message, string mapName, int serverId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    try
                    {
                        SqlCommand query = new SqlCommand("[dbo].[InsertMatchLogRecord]", connection)
                        {
                            CommandType = System.Data.CommandType.StoredProcedure
                        };
                        query.Parameters.AddWithValue("@MATCHID", matchId);
                        query.Parameters.AddWithValue("@MESSAGE", message);
                        query.Parameters.AddWithValue("@MAP", mapName);
                        query.Parameters.AddWithValue("@SERVERID", serverId);
                        await query.ExecuteNonQueryAsync();
                        await connection.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
                    }
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
            }
        }

        public async static Task SetMatchHighlight(string steamId, int killsCount, int matchID, bool isOpenFrag)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[OnMatchHighlight]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    query.Parameters.AddWithValue("@KILLS", killsCount);
                    query.Parameters.AddWithValue("@STEAMID", steamId);
                    query.Parameters.AddWithValue("@MATCHID", matchID);
                    if (isOpenFrag)
                    {
                        query.Parameters.AddWithValue("@OPENFRAG", 1);
                    }
                    else
                    {
                        query.Parameters.AddWithValue("@OPENFRAG", 0);
                    }
                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }

        public async static Task SetHighlight(string steamId, int killsCount)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[OnPlayerHighlight]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    query.Parameters.AddWithValue("@KILLS", killsCount);
                    query.Parameters.AddWithValue("@STEAMID", steamId);
                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }

        public async static Task WeaponKill(string steamId, string weaponName)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[OnPlayerKillByWeapon]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    query.Parameters.AddWithValue("@STEAMID", steamId);
                    query.Parameters.AddWithValue("@WEAPON", weaponName);
                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }

        public async static Task ResetMatch(int matchId, int serverID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[ResetMatch]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    query.Parameters.AddWithValue("@MATCHID", matchId);
                    query.Parameters.AddWithValue("@SERVERID", serverID);
                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }

        public async static Task SetOpenFrag(string steamId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[OnPlayerOpenFrag]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    query.Parameters.AddWithValue("@STEAMID", steamId);
                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }

        private static void SetPlayerMatchResult(string steamId, int win, int pts)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    connection.Open();
                    SqlCommand query = new SqlCommand("[dbo].[SetPlayerMatchResult]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    query.Parameters.AddWithValue("@STEAMID", steamId);
                    query.Parameters.AddWithValue("@WIN", win);
                    query.Parameters.AddWithValue("@PTS", pts);
                    query.ExecuteNonQuery();
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }

        public async static Task InsertMatchBackupRecord(Match match, MatchBackup backup)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[InsertMatchBackupRecord]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    query.Parameters.AddWithValue("@ID", match.MatchId);
                    query.Parameters.AddWithValue("@STEAMID", backup.SteamID);
                    query.Parameters.AddWithValue("@MONEY", backup.Money);
                    query.Parameters.AddWithValue("@PW", backup.PrimaryWeapon);
                    query.Parameters.AddWithValue("@SW", backup.SecondaryWeapon);
                    query.Parameters.AddWithValue("@HE", backup.FragGrenades);
                    query.Parameters.AddWithValue("@FLASHBANGS", backup.Flashbangs);
                    query.Parameters.AddWithValue("@SMOKEGRENADES", backup.SmokeGrenades);
                    query.Parameters.AddWithValue("@HELM", backup.Helm);
                    query.Parameters.AddWithValue("@ARMOR", backup.Armor);
                    query.Parameters.AddWithValue("@DEFUSEKIT", backup.DefuseKit);
                    query.Parameters.AddWithValue("@FRAGS", backup.Frags);
                    query.Parameters.AddWithValue("@DEATHS", backup.Deaths);
                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }
    }
}
