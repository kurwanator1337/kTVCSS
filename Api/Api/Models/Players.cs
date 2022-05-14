using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
	public class Players
	{
		public int ID { get; set; }
		public string NAME { get; set; }
		public string STEAMID { get; set; }
		public double? KILLS { get; set; }
		public double? DEATHS { get; set; }
		public double? HEADSHOTS { get; set; }
		public int? MMR { get; set; }
		public double? MATCHESPLAYED { get; set; }
		public double? MATCHESWINS { get; set; }
		public double? MATCHESLOOSES { get; set; }
		public byte? ISCALIBRATION { get; set; }
		public DateTime? LASTMATCH { get; set; }
		public string? VKID { get; set; }
		public byte? ANOUNCE { get; set; }
		public byte? BLOCK { get; set; }
		public string? BLOCKREASON { get; set; }
	}
}
