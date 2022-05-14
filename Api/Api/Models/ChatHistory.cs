using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Models
{
	public class ChatHistory
	{
		public int ID { get; set; }
		public string? STEAMID { get; set; }
		public string? MESSAGE { get; set; }
		public int? SERVERID { get; set; }
		public DateTime DateTime { get; set; }
	}
}
