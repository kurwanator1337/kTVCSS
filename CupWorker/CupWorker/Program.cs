using System;
using System.Data.SqlClient;
using System.Threading;

namespace CupWorker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "kTVCSS Cup Worker";
            Console.ForegroundColor = ConsoleColor.Green;

            using (SqlConnection connection = new SqlConnection(ProgSettings.Default.SQL))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT ATEAM, BTEAM, SERVERID, DTSTART, DTEND, ID FROM CupWorkerMatches WHERE STATUS = 1", connection);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string ATeam = reader[0].ToString();
                        string BTeam = reader[1].ToString();
                        string ServerID = reader[2].ToString();
                        DateTime DTStart = DateTime.Parse(reader[3].ToString());
                        DateTime DTEnd = DateTime.Parse(reader[4].ToString());
                        string ID = reader[5].ToString();

                        string[] server = Tools.GetServerInfo(ID);

                        Thread controlThread = new Thread(Tools.MatchControl);
                        controlThread.Start(ATeam + ";" + BTeam + ";" + server[0] + ";" + server[1] + ";" + server[2] + ";" + DTStart + ";" + DTEnd);
                    }
                }
            }
        }
    }
}
