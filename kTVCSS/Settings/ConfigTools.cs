using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace kTVCSS.Settings
{
    public class ConfigTools
    {
        public Config Config = new Config();

        public ConfigTools()
        {
            if (!File.Exists("kTVCSS.cfg"))
            {
                Console.WriteLine("Not found application config file, please setup it:");
                Console.WriteLine("SQL Connection String: ");
                Config.SQLConnectionString = Console.ReadLine();
                Console.WriteLine("VK Token: ");
                Config.VKToken = Console.ReadLine();
                CreateSettingsXML(Config);
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

        private XElement CreateSettings(Config config)
        {
            return new XElement("Config", new XElement("SQLConnectionString", config.SQLConnectionString), new XElement("VKToken", config.VKToken));
        }

        public void CreateSettingsXML(Config config)
        {
            var xml = CreateSettings(config);
            xml.Save("kTVCSS.cfg");
        }
    }
}
