using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace WardenCat
{
    internal class Program
    {
        private static List<ProcessInfo> ProcessList = new List<ProcessInfo>();

        private static void GetProcessList()
        {
            string wmiQuery = string.Format("select CommandLine, ProcessId from Win32_Process where Name='{0}'", "kTVCSS.exe");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection retObjectCollection = searcher.Get();
            foreach (ManagementObject retObject in retObjectCollection)
            {
                foreach (Match m in Regex.Matches(retObject["CommandLine"].ToString(), @"(?<Path>"".*?"") (?<Args>.*)", RegexOptions.Multiline))
                {
                    ProcessList.Add(new ProcessInfo()
                    {
                        Path = m.Groups["Path"].Value,
                        Args = m.Groups["Args"].Value,
                        Id = int.Parse(retObject["ProcessId"].ToString())
                    });
                }
            }
        }

        private static void SetHook()
        {
            foreach (ProcessInfo process in ProcessList)
            {
                Process proc = Process.GetProcessById(process.Id);
                proc.EnableRaisingEvents = true;

                proc.Exited += (a, e) =>
                {
                    Process node = new Process();
                    node.StartInfo.UseShellExecute = true;
                    node.StartInfo.FileName = process.Path;
                    node.StartInfo.Arguments = process.Args;
                    node.Start();
                    ProcessList.Clear();
                    GetProcessList();
                    SetHook();
                };
            }
        }

        static void Main(string[] args)
        {
            GetProcessList();

            SetHook();

            Console.WriteLine("Started. If you need to close it press any key to exit");
            Console.ReadKey();
        }
    }
}
