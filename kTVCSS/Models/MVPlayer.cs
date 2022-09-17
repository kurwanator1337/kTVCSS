using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    /// <summary>
    /// MVP матча
    /// </summary>
    public class MVPlayer
    {
        /// <summary>
        /// Ник
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Винрейт
        /// </summary>
        public double WinRate { get; set; }
        /// <summary>
        /// Всего матчей
        /// </summary>
        public int TotalPlayed { get; set; }
        /// <summary>
        /// Победы
        /// </summary>
        public int Won { get; set; }
        /// <summary>
        /// Поражения
        /// </summary>
        public int Lost { get; set; }
        /// <summary>
        /// Процент хедшотов
        /// </summary>
        public double HSR { get; set; }
        /// <summary>
        /// KDR
        /// </summary>
        public double KDR { get; set; }
        /// <summary>
        /// АВГ
        /// </summary>
        public double AVG { get; set; }
        /// <summary>
        /// Название ранга
        /// </summary>
        public string RankName { get; set; }
        /// <summary>
        /// Количество очков рейтинга
        /// </summary>
        public int RankMMR { get; set; }
    }
}
