using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    /// <summary>
    /// Результат матча
    /// </summary>
    public class MatchResultInfo
    {
        /// <summary>
        /// Название карты
        /// </summary>
        public string MapName { get; set; }
        /// <summary>
        /// MVP матча
        /// </summary>
        public MVPlayer MVPlayer { get; set; }
        /// <summary>
        /// Счет матча
        /// </summary>
        public MatchScore MatchScore { get; set; }
        /// <summary>
        /// Список игроков
        /// </summary>
        public List<PlayerResult> PlayerResults = new List<PlayerResult>();
    }
}
