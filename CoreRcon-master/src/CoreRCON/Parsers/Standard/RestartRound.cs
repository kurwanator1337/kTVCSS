using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRCON.Parsers.Standard
{
    public class RestartRound : IParseable
    {
        public bool Restart { get; set; }
    }

    public class RestartRoundParser : DefaultParser<RestartRound>
    {
        public override string Pattern { get; } = @"World triggered ""Restart_Round";

        public override RestartRound Load(GroupCollection groups)
        {
            return new RestartRound
            {
                Restart = true
            };
        }
    }
}
