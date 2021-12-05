using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Tools
{
    public static class OnPlayerConnectAuth
    {
        public static async Task<bool> AuthPlayer(string SteamID, string Name)
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
    }
}
