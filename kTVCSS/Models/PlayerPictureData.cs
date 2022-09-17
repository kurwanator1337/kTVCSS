using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    /// <summary>
    /// Информация об игроке для формирования картинки
    /// </summary>
    public class PlayerPictureData
    {
        /// <summary>
        /// Киллы
        /// </summary>
        public string Kills { get; set; }
        /// <summary>
        /// Смерти
        /// </summary>
        public string Deaths { get; set; }
        /// <summary>
        /// Процент хедшотов
        /// </summary>
        public string HSR { get; set; }
        /// <summary>
        /// Количество очков рейтинга
        /// </summary>
        public string MMR { get; set; }
        /// <summary>
        /// Ранг
        /// </summary>
        public string RankName { get; set; }
        /// <summary>
        /// Матчей сыграно
        /// </summary>
        public string MatchesTotal { get; set; }
        /// <summary>
        /// Победы
        /// </summary>
        public string MatchesWon { get; set; }
        /// <summary>
        /// Поражения
        /// </summary>
        public string MatchesLost { get; set; }
        /// <summary>
        /// КДР
        /// </summary>
        public string KDR { get; set; }
        /// <summary>
        /// АВГ
        /// </summary>
        public string AVG { get; set; }
        /// <summary>
        /// Эйсы
        /// </summary>
        public string Aces { get; set; }
        /// <summary>
        /// Квадры (-4)
        /// </summary>
        public string Quadra { get; set; }
        /// <summary>
        /// Трипплы (-3)
        /// </summary>
        public string Tripple { get; set; }
        /// <summary>
        /// Опенфраги
        /// </summary>
        public string Opens { get; set; }
        /// <summary>
        /// Ник
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Стим айди
        /// </summary>
        public string SteamId { get; set; }
        /// <summary>
        /// Победа?
        /// </summary>
        public bool IsVictory { get; set; }
    }
}
