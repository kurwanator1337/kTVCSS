using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRCON.Parsers.Standard
{
    public class ChangeSides : IParseable
    {
        public string ChangeSidesString { get; set; }
    }

    public class ChangeSidesParser : DefaultParser<ChangeSides>
    {
        public override string Pattern { get; } = @"KTV_CHANGE_SIDES";

        public override ChangeSides Load(GroupCollection groups)
        {
            return new ChangeSides
            {
                ChangeSidesString = "true"
            };
        }
    }
}
