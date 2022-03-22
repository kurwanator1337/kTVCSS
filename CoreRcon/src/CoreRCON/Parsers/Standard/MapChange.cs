using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRCON.Parsers.Standard
{
	public class MapChange : IParseable
	{
		public string Map { get; set; }
	}

	public class MapChangeParser : DefaultParser<MapChange>
	{
		public override string Pattern { get; } = @"Loading map (?<Map>.*)";

		public override MapChange Load(GroupCollection groups)
		{
			return new MapChange
			{
				Map = groups["Map"].Value.Replace("\"", "")
			};
		}
	}
}
