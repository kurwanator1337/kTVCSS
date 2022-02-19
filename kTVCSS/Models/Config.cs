using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace kTVCSS.Models
{
    public class Config
    {
        public string SQLConnectionString { get; set; }
        public string VKToken { get; set; }
        public string SourceBansConnectionString { get; set; }
    }
}
