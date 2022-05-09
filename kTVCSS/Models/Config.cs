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
        public long StatGroupID { get; set; }
        public long MainGroupID { get; set; }
        public long AdminVkID { get; set; }
        public string SSHHost { get; set; }
        public string SSHLogin { get; set; }
        public string SSHPassword { get; set; }
    }
}
