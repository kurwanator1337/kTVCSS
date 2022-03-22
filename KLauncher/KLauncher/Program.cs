using System;
using System.IO;
using System.Threading;

namespace KLauncher
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            ConfigTools config = new ConfigTools();

            foreach (var item in config.ProcessList.Process)
            {
                System.Diagnostics.Process node = new System.Diagnostics.Process();
                node.StartInfo.UseShellExecute = true;
                node.StartInfo.FileName = item.Path;
                node.StartInfo.Arguments = item.Args;
                node.StartInfo.WorkingDirectory = Path.GetDirectoryName(item.Path);
                node.Start();
                Thread.Sleep(2000);
            }
        }
    }
}
