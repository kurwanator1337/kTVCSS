using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Game
{
    /// <summary>
    /// Постоянные переменные
    /// </summary>
    public static class Cvars
    {
        /// <summary>
        /// Фризтайм матча
        /// </summary>
        public const int FREEZETIME_MATCH = 10;
        /// <summary>
        /// Фризтайм микса
        /// </summary>
        public const int FREEZETIME_MIX = 5; // 5
        /// <summary>
        /// Перерыв матча
        /// </summary>
        public const int HALF_TIME_PERIOD_MATCH = 60;
        /// <summary>
        /// Перерыв микса
        /// </summary>
        public const int HALF_TIME_PERIOD_MIX = 5; // 10
        /// <summary>
        /// Перерыв оверов матча
        /// </summary>
        public const int HALF_TIME_PERIOD_MATCH_OVERTIME = 30;
        /// <summary>
        /// Перерыв оверов микса
        /// </summary>
        public const int HALF_TIME_PERIOD_MIX_OVERTIME = 5; // 5
        /// <summary>
        /// ФФ матча
        /// </summary>
        public const int FRIENDLYFIRE_MATCH = 1;
        /// <summary>
        /// ФФ микса
        /// </summary>
        public const int FRIENDLYFIRE_MIX = 0; // 0
    }
}
