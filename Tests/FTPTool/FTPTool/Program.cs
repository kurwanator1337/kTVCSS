using FluentFTP;
using Renci.SshNet;
using System;
using System.IO;
using System.IO.Compression;

namespace FTPTool
{
    public class Server
    {
        public int ID { get; set; }
        public string Host { get; set; }
        public string UserName { get; set; }
        public string UserPassword { get; set; }
        public ushort Port { get; set; }
        public ushort GamePort { get; set; }
        public string RconPassword { get; set; }
        public string NodeHost { get; set; }
        public ushort NodePort { get; set; }
    }

    public class FTPTools
    {
        public string AHost { get; set; }
        public string AUserName { get; set; }
        public string APassword { get; set; }

        public FTPTools(Server server)
        {
            AHost = server.Host;
            AUserName = server.UserName;
            APassword = server.UserPassword;
        }

        public void DownloadFile(string fileName)
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

        public void UploadFile(string fileName)
        {
            try
            {
                using (SftpClient client = new SftpClient("host", "root", "test"))
                {
                    client.Connect();
                    using (FileStream filestream = File.OpenRead(Path.Combine("demos", fileName)))
                    {
                        client.UploadFile(filestream, "/home/web/publish/wwwroot/demos/" + fileName, true);
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

    internal class Program
    {
        static void Main(string[] args)
        {
            FTPTools ftp = new FTPTools(new Server() { Host = "host", UserName = "user", UserPassword = "password" });
            //ftp.DownloadFile("2022-03-24_20_01_52_de_cache_csgo.dem");
            ftp.UploadFile("2022-03-24_20_01_52_de_cache_csgo.dem.zip");
        }
    }
}
