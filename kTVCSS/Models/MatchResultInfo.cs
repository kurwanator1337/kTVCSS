using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    public class MatchResultInfo
    {
        public string MapName { get; set; }
        public MVPlayer MVPlayer { get; set; }
        public MatchScore MatchScore { get; set; }
        public List<PlayerResult> PlayerResults = new List<PlayerResult>();
    }
}
