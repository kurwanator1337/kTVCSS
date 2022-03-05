using kTVCSS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.VKInteraction
{
    public static class Matches
    {
        public static async Task<MVPlayer> GetMatchMVP(int ID)
        {
            MVPlayer player = new MVPlayer();
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT NAME, WINRATE, MATCHESPLAYED, MATCHESWINS, MATCHESLOOSES, HSR, KDR, AVG, RANKNAME, MMR FROM Players INNER JOIN MatchesMVP ON Players.STEAMID = MatchesMVP.MVP WHERE MatchesMVP.ID = {ID}", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                player.Name = reader.GetString(0);
                                player.WinRate = reader.GetDouble(1);
                                player.TotalPlayed = reader.GetInt32(2);
                                player.Won = reader.GetInt32(3);
                                player.Lost = reader.GetInt32(4);
                                player.HSR = reader.GetDouble(5);
                                player.KDR = reader.GetDouble(6);
                                player.AVG = reader.GetDouble(7);
                                player.RankName = reader.GetString(8);
                                player.RankMMR = reader.GetInt32(9);
                            }
                            catch (Exception ex)
                            {
                                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
                            }
                        }
                    }
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
            return player;
        }
        public static async Task<List<PlayerResult>> GetPlayerResults(int ID)
        {
            List<PlayerResult> results = new List<PlayerResult>();
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT NAME, KILLS, DEATHS FROM MatchesResults WHERE ID = {ID}", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                PlayerResult playerResult = new PlayerResult
                                {
                                    Name = reader.GetString(0),
                                    Kills = reader.GetDouble(1),
                                    Deaths = reader.GetDouble(2)
                                };
                                playerResult.KDR = playerResult.Kills / playerResult.Deaths;
                                results.Add(playerResult);
                            }
                            catch (Exception ex)
                            {
                                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
                            }
                        }
                    }
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, ex.Message, LogLevel.Error);
            }
            return results;
        }
    }
}
