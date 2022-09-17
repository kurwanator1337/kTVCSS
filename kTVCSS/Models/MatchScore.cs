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
    public class MatchScore
    {
        /// <summary>
        /// Название команды А
        /// </summary>
        public string AName { get; set; }
        /// <summary>
        /// Название команды Б
        /// </summary>
        public string BName { get; set; }
        /// <summary>
        /// Счет команды А
        /// </summary>
        public string AScore { get; set; }
        /// <summary>
        /// Счет команды Б
        /// </summary>
        public string BScore { get; set; }
    }
}
