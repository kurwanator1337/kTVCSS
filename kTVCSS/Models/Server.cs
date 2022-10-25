using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Models
{
    /// <summary>
    /// Сервер
    /// </summary>
    public class Server
    {
        /// <summary>
        /// Айди сервера
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// Адрес
        /// </summary>
        public string Host { get; set; }
        /// <summary>
        /// ФТП имя пользователя
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// ФТП пароль
        /// </summary>
        public string UserPassword { get; set; }
        /// <summary>
        /// ФТП порт
        /// </summary>
        public ushort Port { get; set; }
        /// <summary>
        /// Игровой порт
        /// </summary>
        public ushort GamePort { get; set; }
        /// <summary>
        /// РКОН пароль
        /// </summary>
        public string RconPassword { get; set; }
        /// <summary>
        /// Неиспользуемое поле, используйте 127.0.0.1
        /// </summary>
        public string NodeHost { get; set; }
        /// <summary>
        /// Порт ноды-обработчика
        /// </summary>
        public ushort NodePort { get; set; }
        /// <summary>
        /// Тип сервера
        /// </summary>
        public ServerType ServerType { get; set; }
    }
}
