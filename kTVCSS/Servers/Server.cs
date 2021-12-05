using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Servers
{
    public class Server
    {
        public int ID { get; set; }
        public string Host { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public ushort Port { get; set; }
        public ushort GamePort { get; set; }
        public string RconPassword { get; set; }
        public string NodeHost { get; set; }
        public ushort NodePort { get; set; }
    }
}
