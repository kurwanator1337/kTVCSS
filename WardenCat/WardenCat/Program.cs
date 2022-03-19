using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;

namespace WardenCat
{
    internal class Program
    {
        private static List<ProcessInfo> ProcessList = new List<ProcessInfo>();

        private static void GetProcessList(string exeName)
        {
            string wmiQuery = string.Format("select CommandLine, ProcessId from Win32_Process where Name='{0}'", exeName);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection retObjectCollection = searcher.Get();
            foreach (ManagementObject retObject in retObjectCollection)
            {
                foreach (Match m in Regex.Matches(retObject["CommandLine"].ToString(), @"(?<Path>"".*?"") (?<Args>.*)", RegexOptions.Multiline))
                {
                    if (!ProcessList.Where(x => x.Path == m.Groups["Path"].Value && x.Args == m.Groups["Args"].Value).Any())
                    {
                        var pinfo = new ProcessInfo()
                        {
                            Path = m.Groups["Path"].Value,
                            Args = m.Groups["Args"].Value,
                            Id = int.Parse(retObject["ProcessId"].ToString())
                        };
                        ProcessList.Add(pinfo);
                        SetHook(pinfo);
                    }
                }
            }
        }

        private static void SetHook(ProcessInfo process)
        {
            System.Diagnostics.Process proc = System.Diagnostics.Process.GetProcessById(process.Id);
            proc.EnableRaisingEvents = true;

            proc.Exited += (a, e) =>
            {
                System.Diagnostics.Process node = new System.Diagnostics.Process();
                node.StartInfo.UseShellExecute = true;
                node.StartInfo.FileName = process.Path;
                node.StartInfo.Arguments = process.Args;
                node.StartInfo.WorkingDirectory = Path.GetDirectoryName(process.Path);
                node.Start();
                ProcessList.RemoveAll(x => x.Id == process.Id);
                GetConfig();
            };
        }

        private static void GetConfig()
        {
            ConfigTool config = new ConfigTool();
            foreach (var item in config.ProcessList.Process)
            {
                GetProcessList(item.Name);
            }
        }

        static void Main(string[] args)
        {
            GetConfig();

            Console.WriteLine("Started. If you need to close it press any key to exit");
            Console.ReadKey();
        }
    }
}
