using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
	public class MatchesBackups
	{
		public int ID { get; set; }
		public string STEAMID { get; set; }
		public string MONEY { get; set; }
		public string PRIMARYWEAPON { get; set; }
		public string SECONDARYWEAPON { get; set; }
		public int FRAGGRENADES { get; set; }
		public int FLASHBANGS { get; set; }
		public int SMOKEGRENADES { get; set; }
		public int HELM { get; set; }
		public int ARMOR { get; set; }
		public int DEFUSEKIT { get; set; }
		public int FRAGS { get; set; }
		public int DEATHS { get; set; }
	}
}
