using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    /// <summary>
    /// Результат игрока за матч
    /// </summary>
    public class PlayerResult
    {
        /// <summary>
        /// Ник
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Киллы
        /// </summary>
        public double Kills { get; set; }
        /// <summary>
        /// Смерти
        /// </summary>
        public double Deaths { get; set; }
        /// <summary>
        /// КДР
        /// </summary>
        public double KDR { get; set; }
        /// <summary>
        /// Название команды
        /// </summary>
        public string TeamName { get; set; }
    }
}
