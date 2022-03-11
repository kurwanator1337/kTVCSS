using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace CupWorker
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.Title = "kTVCSS Cup Worker";
            Console.ForegroundColor = ConsoleColor.Green;

            using (SqlConnection connection = new SqlConnection(ProgSettings.Default.SQL))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT ATEAM, BTEAM, SERVERID, DTEND, ID FROM CupWorkerMatches WHERE STATUS = 1", connection);
                using (SqlDataReader reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string ATeam = reader[0].ToString();
                        string BTeam = reader[1].ToString();
                        string ServerID = reader[2].ToString();
                        DateTime DTEnd = DateTime.Parse(reader[3].ToString());
                        string ID = reader[4].ToString();

                        string[] server = Tools.GetServerInfo(ServerID);

                        if (DateTime.Compare(DateTime.Now, DTEnd) < 0)
                        {
                            BackgroundWorker worker = new BackgroundWorker();
                            worker.DoWork += Worker_DoWork;
                            worker.RunWorkerAsync($"{ATeam};{BTeam};{server[0]};{int.Parse(server[1])};{server[2]};{DTEnd};{ID}");
                        }
                    }
                }
            }

            while (true)
            {
                using (SqlConnection connection = new SqlConnection(ProgSettings.Default.SQL))
                {
                    connection.Open();
                    SqlCommand query = new SqlCommand($"SELECT ATEAM, BTEAM, SERVERID, DTSTART, DTEND, ID FROM CupWorkerMatches WHERE STATUS = 0", connection);
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
                            if (DateTime.Compare(DateTime.Now, DTStart) >= 0)
                            {
                                Tools.UpdateMatchStatus(ID);

                                string[] server = Tools.GetServerInfo(ServerID);

                                BackgroundWorker worker = new BackgroundWorker();
                                worker.DoWork += Worker_DoWork;
                                worker.RunWorkerAsync($"{ATeam};{BTeam};{server[0]};{int.Parse(server[1])};{server[2]};{DTEnd};{ID}");
                            }
                        }
                    }
                }
                await Task.Delay(30000);
            }
        }

        private static async void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument.ToString().Split(';');
            await Tools.MatchControl(args[0], args[1], args[2], int.Parse(args[3]), args[4], DateTime.Parse(args[5]), args[6]);
        }
    }
}
