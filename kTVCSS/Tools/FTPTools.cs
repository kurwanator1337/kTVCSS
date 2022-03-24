using FluentFTP;
using kTVCSS.Models;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Tools
{
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
                using (SftpClient client = new SftpClient(Program.ConfigTools.Config.SSHHost, Program.ConfigTools.Config.SSHLogin, Program.ConfigTools.Config.SSHPassword))
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
}
