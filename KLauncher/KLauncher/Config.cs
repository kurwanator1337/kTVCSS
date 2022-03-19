using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace KLauncher
{
    [XmlRoot(ElementName = "Process")]
    public class Process
    {
        [XmlElement(ElementName = "Path")]
        public string Path { get; set; }

        [XmlElement(ElementName = "Args")]
        public string Args { get; set; }
    }

    [XmlRoot(ElementName = "Config")]
    public class Config
    {
        [XmlElement(ElementName = "Process")]
        public List<Process> Process { get; set; }
    }

    public class ConfigTools
    {
        public Config ProcessList = new Config();

        public ConfigTools()
        {
            ProcessList = new Config();
            XmlSerializer serializer = new XmlSerializer(typeof(Config));
            using (StringReader reader = new StringReader(File.ReadAllText("kTVCSS.cfg")))
            {
                ProcessList = (Config)serializer.Deserialize(reader);
            }
        }
    }
}
