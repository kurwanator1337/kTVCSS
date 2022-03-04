using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRCON.Parsers.Standard
{
    public class NotLive : IParseable
    {
        public string NotLiveString { get; set; }
    }

    public class NotLiveParser : DefaultParser<NotLive>
    {
        public override string Pattern { get; } = @"World triggered NotLive";

        public override NotLive Load(GroupCollection groups)
        {
            return new NotLive
            {
                NotLiveString = "true"
            };
        }
    }
}
