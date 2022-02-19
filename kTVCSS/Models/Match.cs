using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    public class Match
    {
        public Match(int mr)
        {
            if (mr == 0)
            {
                MaxRounds = mr;
                IsMatch = false;
                FirstHalf = true;
                AScore = 0;
                BScore = 0;
                RoundID = 0;
                OpenFragSteamID = string.Empty;
                IsOvertime = false;
                AScoreOvertime = 0;
                BScoreOvertime = 0;
                TacticalPauses = 0;
                TechnicalPauses = 0;
                Pause = false;
            }
            else
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
                TacticalPauses = 4;
                TechnicalPauses = 2;
                Pause = false;
            }
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
        public int TechnicalPauses { get; set; }
        public int TacticalPauses { get; set; }
        public bool Pause { get; set; }
    }
}
