using System.Text.RegularExpressions;

namespace CoreRCON.Parsers.Standard
{
    public class CancelMatch : IParseable
    {
        public string CancelMatchString { get; set; }
    }

    public class CancelMatchParser : DefaultParser<CancelMatch>
    {
        public override string Pattern { get; } = @"World triggered CancelMatch";

        public override CancelMatch Load(GroupCollection groups)
        {
            return new CancelMatch
            {
                CancelMatchString = "true"
            };
        }
    }
}
