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

        public Logger()
        {
            if (!File.Exists("kTVCSS.log"))
            {
                File.WriteAllText("kTVCSS.log", null, Encoding.UTF8);
            }
        }

        public void Print(string message, LogLevel logLevel)
        {
            Console.WriteLine($"{DateTime.Now} [{logLevel}] - {message}");
            using (streamWriter = new StreamWriter("kTVCSS.log", true, Encoding.UTF8))
            {
                streamWriter.WriteLine($"{DateTime.Now} [{logLevel}] - {message}");
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
