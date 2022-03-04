using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRCON.Parsers.Standard
{
    public class MatchBackup : IParseable
    {
        public string SteamID { get; set; }
        public int Money { get; set; }
        public string PrimaryWeapon { get; set; }
        public string SecondaryWeapon { get; set; }
        public int FragGrenades { get; set; }
        public int Flashbangs { get; set; }
        public int SmokeGrenades { get; set; }
        public int Helm { get; set; }
        public int Armor { get; set; }
        public int DefuseKit { get; set; }
        public int Frags { get; set; }
        public int Deaths { get; set; }
    }

    public class MatchBackupParser : DefaultParser<MatchBackup>
    {
        public override string Pattern { get; } = @".KBACKUP] {(?<steam>.+?)};{(?<money>.+?)};{(?<primary_weapon>.+?)};{(?<secondary_weapon>.+?)};{(?<grenades>.+?)};{(?<armor>.+?)};{(?<defuse_kit>.+?)};{(?<frags_deaths>.+?)}";

        public override MatchBackup Load(GroupCollection groups)
        {
            return new MatchBackup
            {
                SteamID = groups["steam"].Value.Replace("|", ":"),
                Money = int.Parse(groups["money"].Value),
                PrimaryWeapon = groups["primary_weapon"].Value,
                SecondaryWeapon = groups["secondary_weapon"].Value,
                FragGrenades = int.Parse(groups["grenades"].Value.Split(',')[0].Trim()),
                Flashbangs = int.Parse(groups["grenades"].Value.Split(',')[1].Trim()),
                SmokeGrenades = int.Parse(groups["grenades"].Value.Split(',')[2].Trim()),
                Helm = int.Parse(groups["armor"].Value.Split(',')[0].Trim()),
                Armor = int.Parse(groups["armor"].Value.Split(',')[1].Trim()),
                DefuseKit = int.Parse(groups["defuse_kit"].Value),
                Frags = int.Parse(groups["frags_deaths"].Value.Split(',')[0].Trim()),
                Deaths = int.Parse(groups["frags_deaths"].Value.Split(',')[1].Trim())
            };
        }
    }
}
