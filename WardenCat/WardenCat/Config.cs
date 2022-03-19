using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WardenCat
{
    [XmlRoot(ElementName = "Process")]
    public class Process
    {
        [XmlElement(ElementName = "Name")]
        public string Name { get; set; }
    }

    [XmlRoot(ElementName = "Config")]
    public class Config
    {
        [XmlElement(ElementName = "Process")]
        public List<Process> Process { get; set; }
    }

    public class ConfigTool
    {
        public Config ProcessList = new Config();

        public ConfigTool()
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
