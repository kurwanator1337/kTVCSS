using CoreRCON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace kTVCSS.Tools
{
    public static class RconHelper
    {
        public static async Task SendMessage(RCON rcon, string message)
        {
            try
            {
                await rcon.SendCommandAsync("sys_say {white}" + message);
            }
            catch (Exception ex)
            {
                Program.Logger.Print(0, ex.Message, LogLevel.Error);
                await RconReconnect(rcon);
            }
        }

        public static async Task SendCmd(RCON rcon, string cmd)
        {
            try
            {
                await rcon.SendCommandAsync(cmd);
            }
            catch (Exception ex)
            {
                Program.Logger.Print(0, ex.Message, LogLevel.Error);
                await RconReconnect(rcon);
            }
        }

        public static async Task LiveOnThree(RCON rcon)
        {
            try
            {
                await rcon.SendCommandAsync("exec ktvcss/ruleset_global.cfg");
                Thread.Sleep(500);
                await rcon.SendCommandAsync("sm_csay LIVE on three restarts!!!");
                await rcon.SendCommandAsync("sys_say {white}LIVE on three restarts!!!;sys_say {white}LIVE on three restarts!!!;sys_say {white}LIVE on three restarts!!!;sys_say {white}LIVE on three restarts!!!;sys_say {white}LIVE on three restarts!!!;sys_say {white}LIVE on three restarts!!!;sys_say {white}LIVE on three restarts!!!");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("clear;mp_restartgame 1;sys_say {white}Restart 1");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("mp_restartgame 1;sys_say {white}Restart 2");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("mp_restartgame 3;sys_say {white}Restart 3");
                Thread.Sleep(4000);
                await rcon.SendCommandAsync("sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!;sys_say {white}MATCH IS LIVE!!!");
            }
            catch (Exception ex)
            {
                Program.Logger.Print(0, ex.Message, LogLevel.Error);
                await RconReconnect(rcon);
            }
        }

        public static async Task Knives(RCON rcon)
        {
            try
            {
                await rcon.SendCommandAsync("sm_csay KNIVES on three restarts!!!");
                await rcon.SendCommandAsync("sys_say {white}KNIVES on three restarts!!!;sys_say {white}KNIVES on three restarts!!!;sys_say {white}KNIVES on three restarts!!!;sys_say {white}KNIVES on three restarts!!!;sys_say {white}KNIVES on three restarts!!!;sys_say {white}KNIVES on three restarts!!!;sys_say {white}KNIVES on three restarts!!!");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("clear;mp_restartgame 1;sys_say {white}Restart 1");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("mp_restartgame 1;sys_say {white}Restart 2");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("mp_restartgame 3;sys_say {white}Restart 3");
                Thread.Sleep(4000);
                await rcon.SendCommandAsync("sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!;sys_say {white}KNIVES!!!");
            }
            catch (Exception ex)
            {
                Program.Logger.Print(0, ex.Message, LogLevel.Error);
                await RconReconnect(rcon);
            }
        }

        public static async Task RconReconnect(RCON rcon)
        {
            try
            {
                await rcon.ConnectAsync();
            }
            catch (Exception ex)
            {
                Program.Logger.Print(0, ex.Message, LogLevel.Error);
            }
        }
    }
}
