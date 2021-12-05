using System.Text.RegularExpressions;

namespace CoreRCON.Parsers.Standard
{
	public class PlayerConnected : IParseable
	{
		public string Host { get; set; }
		public Player Player { get; set; }
	}

	public class PlayerConnectedParser : DefaultParser<PlayerConnected>
	{
		public override string Pattern { get; } = @"(?<Player>""(?<Name>.+?)<(?<ClientID>\d+?)><(?<SteamID>.+?)><(?<Team>.+?)?>\"" entered the game)";
		private static PlayerParser playerParser { get; } = new PlayerParser();

		public override PlayerConnected Load(GroupCollection groups)
		{
			return new PlayerConnected
			{
				Player = playerParser.Parse(groups["Player"]),
			};
		}
	}
}