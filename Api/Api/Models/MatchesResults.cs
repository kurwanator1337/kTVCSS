using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
	public class MatchesResults
	{
		public int ID { get; set; }
		public string TEAMNAME { get; set; }
		public string NAME { get; set; }
		public string STEAMID { get; set; }
		public int KILLS { get; set; }
		public int DEATHS { get; set; }
		public int HEADSHOTS { get; set; }
		public int SERVERID { get; set; }
	}
}
