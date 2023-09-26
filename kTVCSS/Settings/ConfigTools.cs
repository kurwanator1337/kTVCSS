using kTVCSS.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace kTVCSS.Settings
{
    /// <summary>
    /// Конфиг
    /// </summary>
    public class ConfigTools
    {
        /// <summary>
        /// Сам конфиг
        /// </summary>
        public Config Config = new Config();
        /// <summary>
        /// Создание и загрузка конфига
        /// </summary>
        public ConfigTools()
        {
            if (File.Exists(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "kTVCSS.cfg")))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Config));
                using (XmlReader reader = XmlReader.Create(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "kTVCSS.cfg")))
                {
                    Config = (Config)xmlSerializer.Deserialize(reader);
                }
            }
            else
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(Config));
                using (XmlReader reader = XmlReader.Create("kTVCSS.cfg"))
                {
                    Config = (Config)xmlSerializer.Deserialize(reader);
                }
            }
        }
    }
}
