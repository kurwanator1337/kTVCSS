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
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand("[dbo].[OnPlayerKill]", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                SqlParameter nameParam = new SqlParameter
                {
                    ParameterName = "@KILLERNAME",
                    Value = killerName
                };
                SqlParameter namedParam = new SqlParameter
                {
                    ParameterName = "@KILLEDNAME",
                    Value = killedName
                };
                SqlParameter killerSteamParam = new SqlParameter
                {
                    ParameterName = "@KILLERSTEAM",
                    Value = killerSteamID
                };
                SqlParameter killedSteamParam = new SqlParameter
                {
                    ParameterName = "@KILLEDSTEAM",
                    Value = killedSteamID
                };
               
                SqlParameter killedHeadshotParam = new SqlParameter
                {
                    ParameterName = "@KILLERHS",
                    Value = killerHeadshot
                };

                SqlParameter serverParam = new SqlParameter
                {
                    ParameterName = "@SERVERID",
                    Value = serverId
                };

                SqlParameter matchParam = new SqlParameter
                {
                    ParameterName = "@ID",
                    Value = matchId
                };

                query.Parameters.Add(nameParam);
                query.Parameters.Add(namedParam);
                query.Parameters.Add(killerSteamParam);
                query.Parameters.Add(killedSteamParam);
                query.Parameters.Add(killedHeadshotParam);
                query.Parameters.Add(serverParam);
                query.Parameters.Add(matchParam);
                await query.ExecuteNonQueryAsync();
                await connection.CloseAsync();
            }
        }
    }
}
