using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Localization
{
    public class Locale
    {
        private string name;

        private Dictionary<string, string> values;

        public Locale(string name, Dictionary<string, string> values)
        {
            this.name = name;
            this.values = values;
        }

        public string Name
        {
            get
            {
                return this.name;
            }
        }

        public Dictionary<string, string> Values
        {
            get
            {
                return this.values;
            }
        }
    }
}
