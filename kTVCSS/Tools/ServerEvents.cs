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
    public static class ServerEvents
    {
        public static async Task<ConnectInfo> PrintPlayerInfo(string steamID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT KDR, HSR, MMR, AVG, RANKNAME, WINRATE, ISCALIBRATION, MATCHESPLAYED FROM [dbo].[Players] WHERE STEAMID = '{steamID}'", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ConnectInfo info = new ConnectInfo();

                            double.TryParse(reader[0].ToString(), out double kdr);
                            double.TryParse(reader[1].ToString(), out double hsr);
                            int.TryParse(reader[2].ToString(), out int mmr);
                            double.TryParse(reader[3].ToString(), out double avg);
                            double.TryParse(reader[5].ToString(), out double winrate);
                            int.TryParse(reader[6].ToString(), out int isCalibration);
                            int.TryParse(reader[7].ToString(), out int matchesPlayed);

                            info.KDR = kdr;
                            info.HSR = hsr;
                            info.MMR = mmr;
                            info.AVG = avg;
                            info.WinRate = winrate;
                            info.IsCalibration = isCalibration;
                            info.RankName = reader[4].ToString();
                            info.MatchesPlayed = matchesPlayed;

                            return info;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
            return null;
        }

        public static async Task<bool> IsUserRegistered(string steamID)
        {
            int mPlayed = 0;
            string vkId = string.Empty;

            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT [MATCHESPLAYED], [VKID] FROM [dbo].[Players] WHERE STEAMID = '{steamID}'", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            mPlayed = int.Parse(reader[0].ToString());
                            vkId = reader[1].ToString();
                        }
                    }
                }
                if (mPlayed >= 10 && string.IsNullOrEmpty(vkId))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
            return true;
        }

        public static async Task InsertConnectData(int serverId, PlayerConnectedIPInfo player)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"INSERT INTO PlayersJoinHistory (STEAMID, IP, DATETIME, TYPE, SERVERID) VALUES ('{player.Player.SteamId}', '{player.IP}', GETDATE(), 'CONNECT', '{serverId}');", connection);
                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
            }
        }

        public static async Task InsertConnectData(int serverId, PlayerConnected player)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"INSERT INTO PlayersJoinHistory (STEAMID, IP, DATETIME, TYPE, SERVERID) VALUES ('{player.Player.SteamId}', '{player.Host}', GETDATE(), 'CONNECT', '{serverId}');", connection);
                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
            }
        }

        public static async Task InsertDisconnectData(int serverId, PlayerDisconnected player)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"INSERT INTO PlayersJoinHistory (STEAMID, DATETIME, TYPE, SERVERID, REASON) VALUES ('{player.Player.SteamId}', GETDATE(), N'DISCONNECT', '{serverId}', '{player.Reason}');", connection);
                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(serverId, ex.Message, LogLevel.Error);
            }
        }

        public static async Task<bool> AuthPlayer(string SteamID, string Name)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[OnPlayerConnectAuth]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    SqlParameter nameParam = new SqlParameter
                    {
                        ParameterName = "@NAME",
                        Value = Name
                    };
                    SqlParameter steamParam = new SqlParameter
                    {
                        ParameterName = "@STEAMID",
                        Value = SteamID
                    };
                    query.Parameters.Add(nameParam);
                    query.Parameters.Add(steamParam);
                    var result = await query.ExecuteScalarAsync();
                    await connection.CloseAsync();
                    if (int.Parse(result.ToString()) == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
                return true;
            }
        }

        public static async Task<bool> CheckIsBanned(string steamID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT BLOCK FROM [dbo].[Players] WHERE STEAMID = '{steamID}'", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int.TryParse(reader[0].ToString(), out int ban);

                            if (ban == 1)
                                return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
            return false;
        }

        public static async Task InsertChatMessage(string steamId, string text, int serverId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[InsertChatMessage]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };

                    query.Parameters.AddWithValue("@STEAMID", steamId);
                    query.Parameters.AddWithValue("@MESSAGE", text);
                    query.Parameters.AddWithValue("@SERVERID", serverId);

                    await query.ExecuteNonQueryAsync();
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
        }

        public static async Task<PlayerRank> GetPlayerRank(string steamID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
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
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
            return null;
        }
    }
}
