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
    /// <summary>
    /// Помощь в ркон взаимодействии с сервером
    /// </summary>
    public static class RconHelper
    {
        /// <summary>
        /// Отправить сообщение в чат
        /// </summary>
        /// <param name="rcon">РКОН соединение</param>
        /// <param name="message">Текст сообщения</param>
        /// <param name="color">Цвет сообщения</param>
        /// <returns></returns>
        public static async Task SendMessage(RCON rcon, string message, Colors color)
        {
            try
            {
                await rcon.SendCommandAsync("sys_say {" + color + "}" + message);
                Program.Logger.Print(Program.Node.ServerID, "exec say " + message, LogLevel.Info);
            }
            catch (Exception ex)
            {
                Program.Logger.Print(0, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                await RconReconnect(rcon);
            }
        }
        /// <summary>
        /// Выполнить команду на сервере
        /// </summary>
        /// <param name="rcon">РКОН соединение</param>
        /// <param name="cmd">Команда</param>
        /// <returns></returns>
        public static async Task SendCmd(RCON rcon, string cmd)
        {
            try
            {
                await rcon.SendCommandAsync(cmd);
                Program.Logger.Print(Program.Node.ServerID, "exec " + cmd, LogLevel.Info);
            }
            catch (Exception ex)
            {
                Program.Logger.Print(0, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                await RconReconnect(rcon);
            }
        }
        /// <summary>
        /// Запустить лайв
        /// </summary>
        /// <param name="rcon">РКОН соединение</param>
        /// <param name="match">Матч</param>
        /// <param name="OnlinePlayers">Список онлайн игроков</param>
        /// <param name="type">Тип сервера</param>
        /// <returns></returns>
        public static async Task LiveOnThree(RCON rcon, Match match, List<Player> OnlinePlayers, ServerType type)
        {
            try
            {
                Program.Logger.Print(Program.Node.ServerID, "exec lo3", LogLevel.Info);
                await rcon.SendCommandAsync("exec ktvcss/ruleset_global.cfg");
                Thread.Sleep(500);
                await rcon.SendCommandAsync("sm_csay LIVE on three restarts!!!");
                await rcon.SendCommandAsync("sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!;sys_say {mediumseagreen}LIVE on three restarts!!!");
                Thread.Sleep(2000);
                if (match.IsOvertime)
                {
                    await rcon.SendCommandAsync("exec ktvcss/on_match_overtime.cfg");
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
                if (type == ServerType.ClanMatch)
                {
                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MATCH}");
                    await RconHelper.SendCmd(rcon, $"mp_friendlyfire {Game.Cvars.FRIENDLYFIRE_MATCH}");
                }
                else if (type == ServerType.Mix)
                {
                    await RconHelper.SendCmd(rcon, $"mp_freezetime {Game.Cvars.FREEZETIME_MIX}");
                    await RconHelper.SendCmd(rcon, $"mp_friendlyfire {Game.Cvars.FRIENDLYFIRE_MIX}");
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
                Program.Logger.Print(0, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                await RconReconnect(rcon);
            }
        }
        /// <summary>
        /// Запустить ножевой раунд
        /// </summary>
        /// <param name="rcon">РКОН соединение</param>
        /// <returns></returns>
        public static async Task Knives(RCON rcon)
        {
            try
            {
                Program.Logger.Print(Program.Node.ServerID, "exec ko3", LogLevel.Info);
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
                Program.Logger.Print(0, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                await RconReconnect(rcon);
            }
        }
        /// <summary>
        /// Переподключение по РКОНу
        /// </summary>
        /// <param name="rcon">РКОН соединение</param>
        /// <returns></returns>
        public static async Task RconReconnect(RCON rcon)
        {
            try
            {
                await rcon.ConnectAsync();
                Program.Logger.Print(Program.Node.ServerID, "Reconnected rcon", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                //Program.Logger.Print(0, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
        }
    }
}
