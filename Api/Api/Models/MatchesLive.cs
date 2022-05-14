using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
	public class MatchesLive
	{
		public int ID { get; set; }
		public int ASCORE { get; set; }
		public int BSCORE { get; set; }
		public int SERVERID { get; set; }
		public string? MAP { get; set; }
		public byte FINISHED { get; set; }
	}
}
