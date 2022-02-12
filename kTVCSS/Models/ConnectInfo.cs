using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    public class ConnectInfo
    {
        public double KDR { get; set; }
        public double HSR { get; set; }
        public int MMR { get; set; }
        public double AVG { get; set; }
        public double WinRate { get; set; }
        public string RankName { get; set; }
        public int IsCalibration { get; set; }
        public int MatchesPlayed { get; set; }
    }
}
