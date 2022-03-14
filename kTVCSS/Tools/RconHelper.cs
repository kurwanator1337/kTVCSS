using CoreRCON;
using CoreRCON.Parsers.Standard;
using kTVCSS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static kTVCSS.Game.Sourcemod;

namespace kTVCSS.Tools
{
    public static class RconHelper
    {
        public static async Task SendMessage(RCON rcon, string message, Colors color)
        {
            try
            {
                await rcon.SendCommandAsync("sys_say {" + color + "}" + message);
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

        public static async Task LiveOnThree(RCON rcon, Match match, List<Player> OnlinePlayers)
        {
            try
            {
                await rcon.SendCommandAsync("exec ktvcss/ruleset_global.cfg");
                Thread.Sleep(500);
                await rcon.SendCommandAsync("sm_csay LIVE on three restarts!!!");
                await rcon.SendCommandAsync("sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!");
                Thread.Sleep(2000);
                if (match.IsOvertime)
                {
                    await RconHelper.SendCmd(rcon, "mp_startmoney 10000");
                }
                await rcon.SendCommandAsync("clear;mp_restartgame 1;sys_say {mediumseagreen}Restart 1");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("mp_restartgame 1;sys_say {mediumseagreen}Restart 2");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("mp_restartgame 3;sys_say {mediumseagreen}Restart 3");
                Thread.Sleep(4000);
                await rcon.SendCommandAsync("sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!;sys_say {mediumseagreen}MATCH IS LIVE!!!");
                if (match.IsNeedSetTeamScores)
                {
                    await rcon.SendCommandAsync($"score_set {match.BScore} {match.AScore}");
                    match.IsNeedSetTeamScores = !match.IsNeedSetTeamScores;
                }
                if (!match.FirstHalf)
                {
                    foreach (MatchBackup data in match.Backups)
                    {
                        if (OnlinePlayers.Where(x => x.SteamId == data.SteamID).Any())
                        {
                            await SendCmd(rcon, $"player_score_set {data.SteamID.Replace(":", "|")} {data.Frags} {data.Deaths}");
                        }
                    }
                }
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
                await rcon.SendCommandAsync("sys_say {mediumseagreen}KNIVES on three restarts!!!;sys_say {mediumseagreen}KNIVES on three restarts!!!;sys_say {mediumseagreen}KNIVES on three restarts!!!;sys_say {mediumseagreen}KNIVES on three restarts!!!;sys_say {mediumseagreen}KNIVES on three restarts!!!;sys_say {mediumseagreen}KNIVES on three restarts!!!;sys_say {mediumseagreen}KNIVES on three restarts!!!");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("clear;mp_restartgame 1;sys_say {mediumseagreen}Restart 1");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("mp_restartgame 1;sys_say {mediumseagreen}Restart 2");
                Thread.Sleep(2000);
                await rcon.SendCommandAsync("mp_restartgame 3;sys_say {mediumseagreen}Restart 3");
                Thread.Sleep(4000);
                await rcon.SendCommandAsync("sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!;sys_say {mediumseagreen}KNIVES!!!");
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
