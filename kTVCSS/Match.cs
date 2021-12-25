using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS
{
    public class Match
    {
        public Match(int mr)
        {
            MaxRounds = mr;
            IsMatch = true;
            FirstHalf = true;
            AScore = 0;
            BScore = 0;
            RoundID = 0;
            OpenFragSteamID = string.Empty;
            IsOvertime = false;
            AScoreOvertime = 0;
            BScoreOvertime = 0;
        }

        public int MaxRounds { get; set; }
        public int AScore { get; set; }
        public int BScore { get; set; }
        public bool IsMatch { get; set; }
        public int MatchId { get; set; }
        public bool FirstHalf { get; set; }
        public int RoundID { get; set; }
        public string OpenFragSteamID { get; set; }
        public bool IsOvertime { get; set; }
        public int AScoreOvertime { get; set; }
        public int BScoreOvertime { get; set; }
        public Dictionary<string, int> PlayerKills = new Dictionary<string, int>();
    }
}
