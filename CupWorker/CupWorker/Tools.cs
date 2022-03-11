using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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

        public async static void MatchControl(object args)
        {
            var input = args.ToString().Split(';');
            string tTrueName = input[0];
            string ctTrueName = input[1];
            string host = input[2];
            int port = int.Parse(input[3]);
            string password = input[4];
            DateTime end = DateTime.Parse(input[5]);
            string id = input[6];

            RconSharp.RconClient client = RconSharp.RconClient.Create(host, port);
            await client.ConnectAsync();
            if (await client.AuthenticateAsync(password))
            {
                while (true)
                {
                    string OnlinePlayersQuery = await client.ExecuteCommandAsync("sm_kurwagay");
                    //var stuffFromDb = GetSteams(tTrueName, ctTrueName);

                    //foreach (var item in stuffFromServer)
                    //{
                    //    if (!item.Contains("STEAM")) continue;
                    //    var steam = item.Split(';');
                    //    if (!stuffFromDb.Contains(steam[1]))
                    //    {
                    //        webClient.DownloadString("https://v34.ktvcss.org.ru/rcon.php?address=" + host +
                    //        "&port=" + port +
                    //            "&password=" + password +
                    //        "&command=kickid " + steam[0] + " [kTVCSSCupBot] Access on this match is denied for you");
                    //        Console.WriteLine(DateTime.Now + ": кикаем " + steam[1] + " с сервера " + host + ":" + port);
                    //    }
                    if (end.Day == DateTime.Now.Day && end.Hour == DateTime.Now.Hour && end.Minute == DateTime.Now.Minute)
                    {
                        Console.WriteLine(DateTime.Now + ": завершаем матч " + tTrueName + " против " + ctTrueName);
                        //DeleteMatch(id);
                        return;
                    }
                }

                

                //Thread.Sleep(30000);
            }
        }
    }
}
