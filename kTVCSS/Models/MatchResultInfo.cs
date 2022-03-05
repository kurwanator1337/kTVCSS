using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    public class MatchResultInfo
    {
        public Dictionary<string, string> TeamNames = new Dictionary<string, string>();
        public Match Match { get; set; }
        public string WinnerName { get; set; }
        public string MapName { get; set; }
        public MVPlayer MVPlayer { get; set; }
        public List<PlayerResult> PlayerResults = new List<PlayerResult>();
    }
}
