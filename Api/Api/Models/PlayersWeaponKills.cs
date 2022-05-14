using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
	public class PlayersWeaponKills
	{
		public string STEAMID { get; set; }
		public string WEAPON { get; set; }
		public int COUNT { get; set; }
	}
}
