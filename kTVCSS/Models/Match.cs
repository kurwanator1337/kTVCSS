using CoreRCON.Parsers.Standard;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    /// <summary>
    /// Объект матча
    /// </summary>
    public class Match
    {
        /// <summary>
        /// Создание матча
        /// </summary>
        /// <param name="mr">Количество раундов одной половины</param>
        public Match(int mr, ServerType type)
        {
            MaxRounds = mr;
            FirstHalf = true;
            AScore = 0;
            BScore = 0;
            RoundID = 0;
            OpenFragSteamID = string.Empty;
            IsOvertime = false;
            AScoreOvertime = 0;
            BScoreOvertime = 0;
            Pause = false;
            IsNeedSetTeamScores = false;
            if (type == ServerType.FastCup)
            {
                MinPlayersToStart = 8; // 8
                MinPlayersToStop = 6; // 6
                MaxRounds = 7;
            }
            else
            {
                MinPlayersToStart = 5;
                MinPlayersToStop = 4;
            }
            IsNeedPauseOnPlayerTimeOut = true;
            CanPause = true;

#if DEBUG
            MinPlayersToStart = 0; // 8
            MinPlayersToStop = 0; // 6
#endif

            if (mr == 0)
            {
                IsMatch = false;
                TacticalPauses = 0;
                TechnicalPauses = 0;
            }
            else
            {
                IsMatch = true;
                TacticalPauses = 4;
                TechnicalPauses = 2;
                Stopwatch = new Stopwatch();
                Stopwatch.Start();
            }
        }
        /// <summary>
        /// Количество киллов в раунде от конкретного игрока
        /// </summary>
        public Dictionary<string, int> PlayerKills = new Dictionary<string, int>();
        /// <summary>
        /// Бэкапы, пока не юзается
        /// </summary>
        public List<MatchBackup> Backups = new List<MatchBackup>();
        /// <summary>
        /// Минимальное количество игроков для старта матча
        /// </summary>
        public int MinPlayersToStart { get; set; }
        /// <summary>
        /// Минимальное количество игроков для остановки матча
        /// </summary>
        public int MinPlayersToStop { get; set; }
        /// <summary>
        /// Количество раундов одной половины матча
        /// </summary>
        public int MaxRounds { get; set; }
        /// <summary>
        /// Счет команды А
        /// </summary>
        public int AScore { get; set; }
        /// <summary>
        /// Счет команды Б
        /// </summary>
        public int BScore { get; set; }
        /// <summary>
        /// Статус матча (активен/неактивен)
        /// </summary>
        public bool IsMatch { get; set; }
        /// <summary>
        /// ID матча
        /// </summary>
        public int MatchId { get; set; }
        /// <summary>
        /// Первая половина?
        /// </summary>
        public bool FirstHalf { get; set; }
        /// <summary>
        /// ID раунда
        /// </summary>
        public int RoundID { get; set; }
        /// <summary>
        /// Стим айди игрока, давшего опен фраг
        /// </summary>
        public string OpenFragSteamID { get; set; }
        /// <summary>
        /// Овертайм?
        /// </summary>
        public bool IsOvertime { get; set; }
        /// <summary>
        /// Счет команды А в овертайме
        /// </summary>
        public int AScoreOvertime { get; set; }
        /// <summary>
        /// Счет команды Б в овертайме
        /// </summary>
        public int BScoreOvertime { get; set; }
        /// <summary>
        /// Количество технических пауз
        /// </summary>
        public int TechnicalPauses { get; set; }
        /// <summary>
        /// Количество тактических пауз
        /// </summary>
        public int TacticalPauses { get; set; }
        /// <summary>
        /// Пауза?
        /// </summary>
        public bool Pause { get; set; }
        /// <summary>
        /// Необходимость задания вручную счета команд после первой половины
        /// </summary>
        public bool IsNeedSetTeamScores { get; set; }
        /// <summary>
        /// Ножевой раунд?
        /// </summary>
        public bool KnifeRound { get; set; }
        /// <summary>
        /// Нужда в паузе по случаю вылета игрока
        /// </summary>
        public bool IsNeedPauseOnPlayerTimeOut { get; set; }
        /// <summary>
        /// Проверка на возможность объявления паузы
        /// </summary>
        public bool CanPause { get; set; }
        /// <summary>
        /// Часики
        /// </summary>
        public Stopwatch Stopwatch { get; set; }
        /// <summary>
        /// Список игроков, покинувших сервер раньше времени
        /// </summary>
        public Dictionary<string, DateTime> DisconnectedPlayers = new Dictionary<string, DateTime>();
    }
}
