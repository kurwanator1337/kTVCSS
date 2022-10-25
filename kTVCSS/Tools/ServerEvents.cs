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
    /// <summary>
    /// Серверные события
    /// </summary>
    public static class ServerEvents
    {
        public static async Task<bool> IsUserTeamMember(string steamID)
        {
            bool isMembered = false;
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT STEAMID FROM [dbo].[TeamsMembers] WHERE STEAMID = '{steamID}'", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string steamId = reader[0].ToString();
                            isMembered = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
            return isMembered;
        }
        /// <summary>
        /// Напечатать инфу об игроке
        /// </summary>
        /// <param name="steamID">Стим айди</param>
        /// <returns></returns>
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
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
            return null;
        }
        /// <summary>
        /// Проверка на наличие игрока в системе
        /// </summary>
        /// <param name="steamID">Стим айди</param>
        /// <returns></returns>
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
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
            return true;
        }
        /// <summary>
        /// Вставить лог об коннекте игрока
        /// </summary>
        /// <param name="serverId">Айди сервера</param>
        /// <param name="player">Игрок</param>
        /// <returns></returns>
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
                Program.Logger.Print(serverId, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
        }
        /// <summary>
        /// Вставить лог коннекта игрока
        /// </summary>
        /// <param name="serverId">Айди сервера</param>
        /// <param name="player">Игрок</param>
        /// <returns></returns>
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
                Program.Logger.Print(serverId, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
        }
        /// <summary>
        /// Вставить лог о дисконнекте игрока
        /// </summary>
        /// <param name="serverId">Айди сервера</param>
        /// <param name="player">Игрок</param>
        /// <returns></returns>
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
                Program.Logger.Print(serverId, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
        }
        /// <summary>
        /// Авторизация игрока
        /// </summary>
        /// <param name="SteamID">Стим айди</param>
        /// <param name="Name">Ник</param>
        /// <returns></returns>
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
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                return true;
            }
        }
        /// <summary>
        /// Проверка на бан игрока
        /// </summary>
        /// <param name="steamID">Стим айди</param>
        /// <returns></returns>
        public static async Task<Dictionary<bool, string>> CheckIsBanned(string steamID)
        {
            Dictionary<bool, string> result = new Dictionary<bool, string>();
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT BLOCK, BLOCKREASON FROM [dbo].[Players] WHERE STEAMID = '{steamID}'", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int.TryParse(reader[0].ToString(), out int ban);

                            if (ban == 1)
                            {
                                result.Add(true, reader[1].ToString());
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
            result.Add(false, null);
            return result;
        }
        /// <summary>
        /// Вставить сообщение игрока в БД
        /// </summary>
        /// <param name="steamId">Стим айди</param>
        /// <param name="text">Текст сообщения</param>
        /// <param name="serverId">Айди сервера</param>
        /// <returns></returns>
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
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
        }
        /// <summary>
        /// Получить ранг игрока
        /// </summary>
        /// <param name="steamID">Стим айди</param>
        /// <returns></returns>
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
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
            return null;
        }
    }
}
