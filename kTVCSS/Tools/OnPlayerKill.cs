using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Tools
{
    public static class OnPlayerKill
    {
        public static async Task SetValues(string killerName, string killedName, string killerSteamID, string killedSteamID, int killerHeadshot, int serverId, int matchId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand("[dbo].[OnPlayerKill]", connection)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    query.Parameters.AddWithValue("@KILLERNAME", killerName);
                    query.Parameters.AddWithValue("@KILLEDNAME", killedName);
                    query.Parameters.AddWithValue("@KILLERSTEAM", killerSteamID);
                    query.Parameters.AddWithValue("@KILLEDSTEAM", killedSteamID);
                    query.Parameters.AddWithValue("@KILLERHS", killerHeadshot);
                    query.Parameters.AddWithValue("@SERVERID", serverId);
                    query.Parameters.AddWithValue("@ID", matchId);

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
