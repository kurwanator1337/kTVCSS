using CoreRCON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Tools
{
    public static class RconHelper
    {
        public static async Task SendMessage(RCON rcon, string message)
        {
            try
            {
                await rcon.SendCommandAsync("say " + message);
            }
            catch (Exception ex)
            {
                Program.Logger.Print(ex.Message, LogLevel.Error);
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
                Program.Logger.Print(ex.Message, LogLevel.Error);
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
                Program.Logger.Print(ex.Message, LogLevel.Error);
            }
        }
    }
}
