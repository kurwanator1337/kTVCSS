using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CupWorker
{
    public static class Tools
    {
        public static string[] GetServerInfo(string id)
        {
            List<string> result = new List<string>();
            var connection = new SqlConnection(ProgSettings.Default.SQL);
            connection.Open();
            var query = new SqlCommand($"SELECT HOST, GAMEPORT, RCONPASSWORD FROM GameServers WHERE ID = {id}", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                result.Add(reader[0].ToString());
                result.Add(reader[1].ToString());
                result.Add(reader[2].ToString());
            }
            connection.Close();
            return result.ToArray();
        }

        private static string GetSteams(string teamA, string teamB)
        {
            string staff = string.Empty;
            var connection = new SqlConnection(ProgSettings.Default.SQL);
            connection = new SqlConnection(ProgSettings.Default.SQL);
            connection.Open();
            var query = new SqlCommand($"SELECT STAFF FROM CupWorkerTeams WHERE NAME = '{teamA}' OR NAME = '{teamB}'", connection);
            var reader = query.ExecuteReader();
            while (reader.Read())
            {
                staff += reader[0].ToString();
            }
            connection.Close();
            return staff;
        }

        public static void DeleteMatch(string id)
        {
            var connection = new SqlConnection(ProgSettings.Default.SQL);
            connection.Open();
            var query = new SqlCommand($"DELETE FROM CupWorkerMatches WHERE ID = {id}", connection);
            query.ExecuteNonQuery();
            connection.Close();
        }

        public static void UpdateMatchStatus(string id)
        {
            var connection = new SqlConnection(ProgSettings.Default.SQL);
            connection.Open();
            var query = new SqlCommand($"UPDATE CupWorkerMatches SET STATUS = 1 WHERE ID = {id}", connection);
            query.ExecuteNonQuery();
            connection.Close();
        }

        public async static Task MatchControl(string tTrueName, string ctTrueName, string host, int port, string password, DateTime end, string id)
        {
            RconSharp.RconClient client = RconSharp.RconClient.Create(host, port);
            client.ConnectionClosed += Client_ConnectionClosed;

            try
            {
                await client.ConnectAsync();

                if (await client.AuthenticateAsync(password))
                {
                    while (true)
                    {
                        string onlinePlayersQuery = await client.ExecuteCommandAsync("sm_idlist");

                        Dictionary<int, string> onlinePlayers = new Dictionary<int, string>();

                        foreach (Match m in Regex.Matches(onlinePlayersQuery, @".*?;.*", RegexOptions.Multiline))
                        {
                            onlinePlayers.Add(int.Parse(m.Value.Split(';')[0]), m.Value.Split(';')[1]);
                        }

                        string databasePlayers = GetSteams(tTrueName, ctTrueName);

                        foreach (var item in onlinePlayers)
                        {
                            if (!databasePlayers.Contains(item.Value))
                            {
                                await client.ExecuteCommandAsync("kickid " + item.Key + " [kTVCSS] Вам запрещен доступ на матч");
                            }
                        }

                        if (DateTime.Compare(DateTime.Now, end) >= 0)
                        {
                            DeleteMatch(id);
                            return;
                        }

                        await Task.Delay(30000);
                    }
                }
            }
            catch (Exception)
            {
                await Task.Delay(10000);
                Client_ConnectionClosed();
            }
        }

        private static void Client_ConnectionClosed()
        {
            Process it = Process.GetCurrentProcess();

            Process node = new Process();
            node.StartInfo.UseShellExecute = true;
            node.StartInfo.FileName = Environment.CurrentDirectory + @"\" + it.ProcessName;
            node.Start();

            it.Kill();
        }
    }
}
