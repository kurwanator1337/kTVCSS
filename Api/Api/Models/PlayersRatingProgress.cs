using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
	public class PlayersRatingProgress
	{
		public string? STEAMID { get; set; }
		public int? MMR { get; set; }
		public DateTime DateTime { get; set; }
	}
}
