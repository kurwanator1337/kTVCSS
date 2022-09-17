using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    /// <summary>
    /// Ранг игрока
    /// </summary>
    public class PlayerRank
    {
        /// <summary>
        /// Стим айди
        /// </summary>
        public string SteamID { get; set; }
        /// <summary>
        /// Очки рейтинга
        /// </summary>
        public int Points { get; set; }
    }
}
