using System.Text.RegularExpressions;
using CoreRCON.Parsers.Standard;

namespace CoreRCON.Parsers.Standard
{
    public class RoundStart : IParseable
    {
        public bool Start { get; set; }
    }

    public class RoundStartParser : DefaultParser<RoundStart>
    {
        public override string Pattern { get; } = @"World triggered ""Round_Start""";

        public override RoundStart Load(GroupCollection groups)
        {
            return new RoundStart
            {
                Start = true
            };
        }
    }
}