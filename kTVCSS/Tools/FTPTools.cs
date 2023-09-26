using FluentFTP;
using kTVCSS.Models;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace kTVCSS.Tools
{
    /// <summary>
    /// Инструменты для работы с FTP
    /// </summary>
    public class FTPTools
    {
        /// <summary>
        /// Хост
        /// </summary>
        public string AHost { get; set; }
        /// <summary>
        /// Имя пользователя
        /// </summary>
        public string AUserName { get; set; }
        /// <summary>
        /// Пароль
        /// </summary>
        public string APassword { get; set; }
        /// <summary>
        /// Создание инструментов ФТП для работы с игровым сервером
        /// </summary>
        /// <param name="server">Сервер</param>
        public FTPTools(Server server)
        {
            AHost = server.Host;
            AUserName = server.UserName;
            APassword = server.UserPassword;
        }
        /// <summary>
        /// Загрузить демо на сайт
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        public void UploadDemo(object fileName)
        {
            Thread thread = new Thread(UploadDemoFile)
            {
                IsBackground = true
            };
            thread.Start(fileName);
        }
        /// <summary>
        /// Загрузить демо на сайт
        /// </summary>
        /// <param name="fileName">Название файла</param>
        private void UploadDemoFile(object fileName)
        {
            DownloadFile(fileName.ToString() + ".dem");
            UploadFile(fileName.ToString() + ".dem.zip");
        }
        /// <summary>
        /// Скачать демо
        /// </summary>
        /// <param name="fileName">Название файла</param>
        private void DownloadFile(string fileName)
        {
            try
            {
                FtpClient client = new FtpClient(AHost, AUserName, APassword);
                client.AutoConnect();
                if (!Directory.Exists("demos"))
                {
                    Directory.CreateDirectory("demos");
                }
                client.DownloadFile(Path.Combine("demos", fileName), fileName);
                client.DeleteFile(fileName);
                client.Disconnect();
                using (ZipArchive newFile = ZipFile.Open(Path.Combine("demos", fileName + ".zip"), ZipArchiveMode.Create))
                {
                    newFile.CreateEntryFromFile(Path.Combine("demos", fileName), fileName, CompressionLevel.Fastest);
                }
                File.Delete(Path.Combine("demos", fileName));
            }
            catch (Exception)
            {
                // Ignored
            }
        }
        /// <summary>
        /// Загрузить файл на сервер по SFTP
        /// </summary>
        /// <param name="fileName"></param>
        private void UploadFile(string fileName)
        {
            try
            {
                using (SftpClient client = new SftpClient(Program.ConfigTools.Config.SSHHost, 1337, Program.ConfigTools.Config.SSHLogin, Program.ConfigTools.Config.SSHPassword))
                {
                    client.Connect();
                    using (FileStream filestream = File.OpenRead(Path.Combine("demos", fileName)))
                    {
                        client.UploadFile(filestream, @"/home/aspnet/ktvcss.ru/wwwroot/demos/" + fileName, true);
                        client.Disconnect();
                    }
                }
                File.Delete(Path.Combine("demos", fileName));
            }
            catch (Exception)
            {
                // Ignored
            }
        }
    }
}
