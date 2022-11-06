using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    public class PlayerInfoMini
    {
        public static string Get(string SteamId)
        {
            string output = string.Empty;
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                connection.Open();
                using (SqlCommand query = new SqlCommand($"SELECT NAME, ROUND(KDR, 2), ROUND (AVG, 2), MMR, VKID FROM Players WHERE STEAMID = '{SteamId}'", connection))
                {
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader[4].ToString().Length > 1)
                            {
                                output = $"@id{reader[4]} ({reader[0]}) (KDR {reader[1]}, AVG {reader[2]}, PTS {reader[3]})";
                            }
                            else
                            {
                                output = $"{reader[0]} (KDR {reader[1]}, AVG {reader[2]}, PTS {reader[3]}";
                            }
                        }
                    }
                }
            }
            return output;
        }
    }
}
