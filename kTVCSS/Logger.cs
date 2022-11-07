using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet.Model.RequestParams;
using VkNet.Model;
using VkNet;

namespace kTVCSS
{
    /// <summary>
    /// Логгер
    /// </summary>
    public class Logger
    {
        private StreamWriter streamWriter = null;
        /// <summary>
        /// Айди сервера
        /// </summary>
        public int LoggerID = 0;
        /// <summary>
        /// Создать логгер программы
        /// </summary>
        /// <param name="loggerID">Айди сервера</param>
        public Logger(int loggerID)
        {
            string path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), $"kTVCSS_{loggerID}.log");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, null, Encoding.UTF8);
            }
            LoggerID = loggerID;
        }
        /// <summary>
        /// Создать запись в лог
        /// </summary>
        /// <param name="serverID">Айди сервера</param>
        /// <param name="message">Сообщение</param>
        /// <param name="logLevel">Уровень лог сообщения</param>
        public void Print(int serverID, string message, LogLevel logLevel)
        {
            string path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), $"kTVCSS_{serverID}.log");
            Console.WriteLine($"[#{serverID}] {DateTime.Now} [{logLevel}] - {message}");
            try
            {
                using (streamWriter = new StreamWriter(path, true, Encoding.UTF8))
                {
                    streamWriter.WriteLine($"[#{serverID}] {DateTime.Now} [{logLevel}] - {message}");
                }
            }
            catch (Exception)
            {
                // file is busy by another process
            }
            if (logLevel == LogLevel.Error)
            {
                //try
                //{
                //    Task.Factory.StartNew(() =>
                //    {
                //        using (VkApi api = new VkApi())
                //        {
                //            api.AuthorizeAsync(new ApiAuthParams
                //            {
                //                AccessToken = Program.ConfigTools.Config.VKGroupToken,
                //            });
                //            api.Messages.Send(new MessagesSendParams()
                //            {
                //                ChatId = 5,
                //                Message = $"[#{serverID}] {DateTime.Now} [{logLevel}] - {message}",
                //                RandomId = new Random().Next()
                //            });
                //        }
                //    });
                //}
                //catch (Exception)
                //{
                //    // can't send
                //}
            }
        }
    }
    /// <summary>
    /// Уровень логов
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Информация
        /// </summary>
        Info,
        /// <summary>
        /// Предупреждение
        /// </summary>
        Warn,
        /// <summary>
        /// Ошибка
        /// </summary>
        Error,
        /// <summary>
        /// Дебаг отладка
        /// </summary>
        Debug,
        /// <summary>
        /// Релиз отладка
        /// </summary>
        Trace
    }
}
