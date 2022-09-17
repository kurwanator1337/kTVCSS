using kTVCSS.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS
{
    /// <summary>
    /// Информация о соединении игрока
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// Ник
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Стим айди
        /// </summary>
        public string SteamId { get; set; }
        /// <summary>
        /// Айди на сервере
        /// </summary>
        public int ClientId { get; set; }
        /// <summary>
        /// IP адрес
        /// </summary>
        public string IP { get; set; }
    }
    /// <summary>
    /// Контроллер подключения игрока
    /// </summary>
    public static class ConnectionController
    {
        /// <summary>
        /// Соединения
        /// </summary>
        public static List<Connection> Connections = new List<Connection>();
        /// <summary>
        /// Добавить соединение в учет
        /// </summary>
        /// <param name="connection"></param>
        public static void AddItem(Connection connection)
        {
            Connections.Add(connection);
        }
        /// <summary>
        /// Удалить
        /// </summary>
        /// <param name="connection"></param>
        public static void RemoveItem(Connection connection)
        {
            Connections.RemoveAll(x => x.ClientId == connection.ClientId);
        }
        /// <summary>
        /// Запустить проверку на впн и блокировку по айпишникам
        /// </summary>
        /// <param name="data">Соединение игрока</param>
        /// <returns></returns>
        public static async Task<int> ExecuteChecker(Connection data)
        {
            try
            {
                WebClient web = new WebClient();
                Uri uri = new Uri("https://blackbox.ipinfo.app/lookup/" + data.IP.Substring(0, data.IP.IndexOf(":")));
                string result = await web.DownloadStringTaskAsync(uri);
                
                if (result == "Y")
                {
                    string whiteList = string.Empty;
                    whiteList = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "whitelist.txt"), System.Text.Encoding.UTF8);
                    if (!whiteList.Contains(data.SteamId))
                    {
                        Program.Logger.Print(Program.Node.ServerID, $"[VPN CHECK] {data.Name} ({data.IP})", LogLevel.Debug);
                        return 1;
                    }
                }
                else
                {
                    Program.Logger.Print(Program.Node.ServerID, $"[VPN CHECK] {data.Name} ({data.IP}) [NORMAL]", LogLevel.Debug);
                }
                List<string> IpTables = new List<string>();
                IpTables.AddRange(File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "iptables.txt"), System.Text.Encoding.UTF8));
                foreach (var rule in IpTables)
                {
                    if (data.IP.Contains(rule))
                    {
                        string whiteList = string.Empty;
                        whiteList = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "whitelist.txt"), System.Text.Encoding.UTF8);
                        if (!whiteList.Contains(data.SteamId))
                        {
                            Program.Logger.Print(Program.Node.ServerID, $"[IPTABLES] {data.Name} ({data.IP}) [BAN IT!!!]", LogLevel.Debug);
                            return 2;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
            return 0;
        }
    }
}
