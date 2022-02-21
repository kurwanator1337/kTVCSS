using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS
{
    public class Logger
    {
        private StreamWriter streamWriter = null;
        public int LoggerID = 0;

        public Logger(int loggerID)
        {
            if (!File.Exists($"kTVCSS_{loggerID}.log"))
            {
                File.WriteAllText($"kTVCSS_{loggerID}.log", null, Encoding.UTF8);
            }
            LoggerID = loggerID;
        }

        public void Print(int serverID, string message, LogLevel logLevel)
        {
            Console.WriteLine($"[#{serverID}] {DateTime.Now} [{logLevel}] - {message}");
            try
            {
                using (streamWriter = new StreamWriter($"kTVCSS_{LoggerID}.log", true, Encoding.UTF8))
                {
                    streamWriter.WriteLine($"[#{serverID}] {DateTime.Now} [{logLevel}] - {message}");
                }
            }
            catch (Exception)
            {
                // file is busy by another process
            }
        }
    }

    public enum LogLevel
    {
        Info,
        Warn,
        Error,
        Debug,
        Trace
    }
}
