using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CoreRCON.Parsers.Standard
{
	public class PlayerConnectedIPInfo : IParseable
	{
		public string IP { get; set; }
		public Player Player { get; set; }
	}

	public class PlayerConnectedIPInfoParser : DefaultParser<PlayerConnectedIPInfo>
	{
		public override string Pattern { get; } = $"(?<Player>{playerParser.Pattern}) connected, address \"(?<Host>.+?)\"";
		private static PlayerParser playerParser { get; } = new PlayerParser();

		public override PlayerConnectedIPInfo Load(GroupCollection groups)
		{
			return new PlayerConnectedIPInfo
			{
				Player = playerParser.Parse(groups["Player"]),
				IP = groups["Host"].Value
			};
		}
	}
}
