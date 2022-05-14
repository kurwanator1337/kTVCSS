using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
	public class MatchesLogs
	{
		public int MATCHID { get; set; }
		public DateTime DateTime { get; set; }
		public string? MESSAGE { get; set; }
		public string MAP { get; set; }
		public int SERVERID { get; set; }
	}
}
