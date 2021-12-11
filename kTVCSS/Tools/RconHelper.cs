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
            await rcon.SendCommandAsync("say " + message);
        }

        public static async Task SendCmd(RCON rcon, string cmd)
        {
            await rcon.SendCommandAsync(cmd);
        }
    }
}
