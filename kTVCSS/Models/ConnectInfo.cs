using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    /// <summary>
    /// Информация, отображаемая при подключении игрока в чат на сервере
    /// </summary>
    public class ConnectInfo
    {
        /// <summary>
        /// КДР
        /// </summary>
        public double KDR { get; set; }
        /// <summary>
        /// Процентов хедшотов
        /// </summary>
        public double HSR { get; set; }
        /// <summary>
        /// Очки рейтинга
        /// </summary>
        public int MMR { get; set; }
        /// <summary>
        /// АВГ
        /// </summary>
        public double AVG { get; set; }
        /// <summary>
        /// Винрейт
        /// </summary>
        public double WinRate { get; set; }
        /// <summary>
        /// Ранг
        /// </summary>
        public string RankName { get; set; }
        /// <summary>
        /// Калибровка?
        /// </summary>
        public int IsCalibration { get; set; }
        /// <summary>
        /// Всего матчей сыграно
        /// </summary>
        public int MatchesPlayed { get; set; }
    }
}
