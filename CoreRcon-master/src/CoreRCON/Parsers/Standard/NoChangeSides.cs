using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRCON.Parsers.Standard
{
    public class NoChangeSides : IParseable
    {
        public string NoChangeSidesString { get; set; }
    }

    public class NoChangeSidesParser : DefaultParser<NoChangeSides>
    {
        public override string Pattern { get; } = @"KTV_DONTCHANGE_SIDES";

        public override NoChangeSides Load(GroupCollection groups)
        {
            return new NoChangeSides
            {
                NoChangeSidesString = "true"
            };
        }
    }
}
