using kTVCSS.Settings;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kTVCSS.Servers;

namespace kTVCSS.Tools
{
    public static class Loader
    {
        public static void LoadServers()
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                connection.Open();
                using (SqlCommand query = new SqlCommand("SELECT HOST, USERNAME, USERPASSWORD, PORT, GAMEPORT, RCONPASSWORD, NODEHOST, NODEPORT, ID " +
                    "FROM [kTVCSS].[dbo].[GameServers] WHERE ENABLED = 1", connection))
                {
                    using (SqlDataReader reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _ = ushort.TryParse(reader[3]?.ToString(), out ushort port);
                            _ = ushort.TryParse(reader[4]?.ToString(), out ushort gamePort);
                            _ = ushort.TryParse(reader[7]?.ToString(), out ushort nodePort);
                            Program.Servers.Add(new Server()
                            {
                                ID = int.Parse(reader[8].ToString()),
                                Host = reader[0]?.ToString(),
                                UserName = reader[1]?.ToString(),
                                UserPassword = reader[2]?.ToString(),
                                Port = port,
                                GamePort = gamePort,
                                RconPassword = reader[5]?.ToString(),
                                NodeHost = reader[6].ToString(),
                                NodePort = nodePort
                            });
                        }
                    }
                }
                connection.Close();
            }
        }
    }
}
