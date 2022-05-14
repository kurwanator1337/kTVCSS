using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
	public class Matches
	{
		public int ID { get; set; }
		public string ANAME { get; set; }
		public string BNAME { get; set; }
		public int ASCORE { get; set; }
		public int BSCORE { get; set; }
		public DateTime MATCHDATE { get; set; }
		public string MAP { get; set; }
		public int SERVERID { get; set; }
	}
}
