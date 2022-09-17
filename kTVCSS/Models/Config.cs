using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace kTVCSS.Models
{
    /// <summary>
    /// Конфиг программы
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Строка подключения к Microsoft SQL Server
        /// </summary>
        public string SQLConnectionString { get; set; }
        /// <summary>
        /// ВК токен авторизации
        /// </summary>
        public string VKToken { get; set; }
        /// <summary>
        /// ID группы статистики
        /// </summary>
        public long StatGroupID { get; set; }
        /// <summary>
        /// ID основной группы
        /// </summary>
        public long MainGroupID { get; set; }
        /// <summary>
        /// ID админа групп (обоих)
        /// </summary>
        public long AdminVkID { get; set; }
        /// <summary>
        /// Адрес SSH сервера для загрузки демок на сервер
        /// </summary>
        public string SSHHost { get; set; }
        /// <summary>
        /// Логин
        /// </summary>
        public string SSHLogin { get; set; }
        /// <summary>
        /// Пароль
        /// </summary>
        public string SSHPassword { get; set; }
    }
}
