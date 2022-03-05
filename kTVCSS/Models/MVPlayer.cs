using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    public class MVPlayer
    {
        public string Name { get; set; }
        public double WinRate { get; set; }
        public int TotalPlayed { get; set; }
        public int Won { get; set; }
        public int Lost { get; set; }
        public double HSR { get; set; }
        public double KDR { get; set; }
        public double AVG { get; set; }
        public string RankName { get; set; }
        public int RankMMR { get; set; }
    }
}
