using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            string path = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), $"kTVCSS_{loggerID}.log");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, null, Encoding.UTF8);
            }
            LoggerID = loggerID;
        }

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
