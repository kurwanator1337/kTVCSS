using CoreRCON.Parsers.Standard;
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
            MaxRounds = mr;
            FirstHalf = true;
            AScore = 0;
            BScore = 0;
            RoundID = 0;
            OpenFragSteamID = string.Empty;
            IsOvertime = false;
            AScoreOvertime = 0;
            BScoreOvertime = 0;
            Pause = false;
            IsNeedSetTeamScores = false;
            MinPlayersToStart = 8; // 8
            MinPlayersToStop = 6; // 6

#if DEBUG
            MinPlayersToStart = 0; // 8
            MinPlayersToStop = 0; // 6
#endif

            if (mr == 0)
            {
                IsMatch = false;
                TacticalPauses = 0;
                TechnicalPauses = 0;
            }
            else
            {
                IsMatch = true;
                TacticalPauses = 4;
                TechnicalPauses = 2;
            }
        }

        public Dictionary<string, int> PlayerKills = new Dictionary<string, int>();
        public List<MatchBackup> Backups = new List<MatchBackup>();

        public int MinPlayersToStart { get; set; }
        public int MinPlayersToStop { get; set; }

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
        public int TechnicalPauses { get; set; }
        public int TacticalPauses { get; set; }
        public bool Pause { get; set; }
        public bool IsNeedSetTeamScores { get; set; }
        public bool KnifeRound { get; set; }
    }
}
