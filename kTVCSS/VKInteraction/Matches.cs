using CoreRCON.Parsers.Standard;
using kTVCSS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace kTVCSS.VKInteraction
{
    /// <summary>
    /// Матчи для ВК
    /// </summary>
    public static class Matches
    {
        /// <summary>
        /// Отправить сообщение в беседу проекта
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <returns></returns>
        public static async Task SendMessageToConf(string message)
        {
            try
            {
                using (VkApi api = new VkApi())
                {
                    await api.AuthorizeAsync(new ApiAuthParams
                    {
                        AccessToken = Program.ConfigTools.Config.VKGroupToken,
                    });
                    await api.Messages.SendAsync(new MessagesSendParams()
                    {
                        ChatId = 4,
                        Message = message,
                        RandomId = new Random().Next()
                    });
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
        }
        /// <summary>
        /// Получить МВП матча
        /// </summary>
        /// <param name="ID">айди матча</param>
        /// <returns></returns>
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
                                player.Name = reader[0].ToString();
                                player.WinRate = Math.Round(double.Parse(reader[1].ToString()), 2);
                                player.TotalPlayed = int.Parse(reader[2].ToString());
                                player.Won = int.Parse(reader[3].ToString());
                                player.Lost = int.Parse(reader[4].ToString());
                                player.HSR = Math.Round(double.Parse(reader[5].ToString()), 2);
                                player.KDR = Math.Round(double.Parse(reader[6].ToString()), 2);
                                player.AVG = Math.Round(double.Parse(reader[7].ToString()), 2);
                                player.RankName = reader[8].ToString();
                                player.RankMMR = int.Parse(reader[9].ToString());
                            }
                            catch (Exception ex)
                            {
                                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                            }
                        }
                    }
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
            return player;
        }
        /// <summary>
        /// Получить результаты матча
        /// </summary>
        /// <param name="ID">Айди матча</param>
        /// <returns></returns>
        public static async Task<List<PlayerResult>> GetPlayerResults(int ID)
        {
            List<PlayerResult> results = new List<PlayerResult>();
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT NAME, KILLS, DEATHS, TEAMNAME FROM MatchesResults WHERE ID = {ID}", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                PlayerResult playerResult = new PlayerResult
                                {
                                    Name = reader[0].ToString(),
                                    Kills = double.Parse(reader[1].ToString()),
                                    Deaths = double.Parse(reader[2].ToString()),
                                    TeamName = reader[3].ToString()
                                };
                                playerResult.KDR = playerResult.Kills / playerResult.Deaths;
                                playerResult.KDR = Math.Round(playerResult.KDR, 2);
                                results.Add(playerResult);
                            }
                            catch (Exception ex)
                            {
                                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                            }
                        }
                    }
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
            return results;
        }
        /// <summary>
        /// Получить результат матча
        /// </summary>
        /// <param name="ID">Айди матча</param>
        /// <returns></returns>
        public static async Task<MatchScore> GetMatchResult(int ID)
        {
            MatchScore match = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    await connection.OpenAsync();
                    SqlCommand query = new SqlCommand($"SELECT ANAME, BNAME, ASCORE, BSCORE FROM Matches WHERE ID = {ID}", connection);
                    using (var reader = await query.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                match = new MatchScore()
                                {
                                    AName = reader[0].ToString(),
                                    BName = reader[1].ToString(),
                                    AScore = reader[2].ToString(),
                                    BScore = reader[3].ToString()
                                };
                            }
                            catch (Exception ex)
                            {
                                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                            }
                        }
                    }
                    await connection.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
            return match;
        }

        private static string CutMapName(string mapName)
        {
            mapName = mapName.Replace("de_", "");
            mapName = mapName.Replace("_csgo", "");
            return mapName.ToUpper();
        }
        /// <summary>
        /// Отправить картинку с результатом матча игроку
        /// </summary>
        /// <param name="player"></param>
        public static void SendPlayerResult(PlayerPictureData player, int matchId)
        {
            try
            {
                System.Drawing.Image image = System.Drawing.Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "player_result.png"));
                Graphics graphics = Graphics.FromImage(image);
                // upper block
                player.Name = player.Name.Length > 17 ? player.Name[..17] : player.Name;
                Drawing.Tools.DrawText(graphics, player.Name, new Rectangle(220, 139, 0, 200), StringAlignment.Near, 30, Brushes.White, FontStyle.Regular, "Play-Regular");
                Drawing.Tools.DrawText(graphics, DateTime.Now.ToString("dd-MM-yyyy HH:mm"), new Rectangle(220, 242, 0, 200), StringAlignment.Near, 30, Brushes.White, FontStyle.Regular, "Play-Regular");
                // first block
                if (player.IsVictory)
                {
                    Drawing.Tools.DrawText(graphics, "WON", new Rectangle(275, 370, 0, 200), StringAlignment.Center, 36, Brushes.LimeGreen, FontStyle.Bold, "Play-Regular");
                }
                else
                {
                    Drawing.Tools.DrawText(graphics, "LOST", new Rectangle(275, 370, 0, 200), StringAlignment.Center, 36, Brushes.Red, FontStyle.Bold, "Play-Regular");
                }
                Drawing.Tools.DrawText(graphics, player.Kills, new Rectangle(383, 457, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.Deaths, new Rectangle(383, 504, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.HSR, new Rectangle(383, 552, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                // second block
                graphics.DrawImage(Drawing.Tools.GetRankImage(player.RankName), 485, 360, 70, 70);
                Drawing.Tools.DrawText(graphics, player.MMR, new Rectangle(700, 365, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.MatchesTotal, new Rectangle(790, 457, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.MatchesWon, new Rectangle(790, 504, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.MatchesLost, new Rectangle(790, 552, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                // third block 
                Drawing.Tools.DrawText(graphics, player.KDR, new Rectangle(383, 787, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.AVG, new Rectangle(383, 834, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.Aces, new Rectangle(383, 882, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.Quadra, new Rectangle(790, 787, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.Tripple, new Rectangle(790, 834, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");
                Drawing.Tools.DrawText(graphics, player.Opens, new Rectangle(790, 882, 0, 200), StringAlignment.Far, 24, Brushes.White, FontStyle.Bold, "Play-Regular");

                //image.Save("player_result.png", System.Drawing.Imaging.ImageFormat.Png);

                string uploadImage = DateTime.Now.ToString("yyyy-MM-dd_hh_mm_ss") + ".png";

                image.Save(uploadImage, System.Drawing.Imaging.ImageFormat.Png);

                var web = new WebClient();
                var api = new VkApi();
                api.Authorize(new ApiAuthParams
                {
                    AccessToken = Program.ConfigTools.Config.VKToken,
                });

                var uploadServer = api.Photo.GetWallUploadServer(Program.ConfigTools.Config.MainGroupID);
                var result = Encoding.ASCII.GetString(web.UploadFile(uploadServer.UploadUrl, uploadImage));
                var photo = api.Photo.SaveWallPhoto(result, (ulong?)Program.ConfigTools.Config.AdminVkID, (ulong?)Program.ConfigTools.Config.MainGroupID);

                string vkId = string.Empty;

                using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
                {
                    connection.Open();
                    SqlCommand query = new SqlCommand($"SELECT VKID FROM [dbo].[Players] WHERE STEAMID = '{player.SteamId}'", connection);
                    using (var reader = query.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            vkId = reader[0].ToString();
                        }
                    }
                    connection.Close();
                }

                if (!string.IsNullOrEmpty(vkId))
                {
                    var messageParams = new MessagesSendParams
                    {
                        GroupId = (ulong)Program.ConfigTools.Config.MainGroupID,
                        UserId = long.Parse(vkId),
                        RandomId = new Random().Next(),
                        Message = $"Подробнее: https://ktvcss.ru/match/{matchId}",
                        Attachments = new List<MediaAttachment>
                        {
                            photo.FirstOrDefault()
                        }
                    };
                    Thread.Sleep(500);
                    api.Messages.Send(messageParams);
                }

                try
                {
                    File.Delete(uploadImage);
                }
                catch (Exception ex)
                {
                    Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
        }
        /// <summary>
        /// Опубликовать результат матча на стену
        /// </summary>
        /// <param name="matchResultInfo">Результат матча</param>
        public static void PublishResult(MatchResultInfo matchResultInfo, int matchId)
        {
            try
            {
                System.Drawing.Image image = System.Drawing.Image.FromFile(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "Pictures", "template_match_result.png"));
                Graphics graphics = Graphics.FromImage(image);

                string MVPName = matchResultInfo.MVPlayer.Name;
                var justMVPNick = matchResultInfo.MVPlayer.Name.Split(' ');
                if (justMVPNick.Count() > 1)
                {
                    for (int k = 1; k < justMVPNick.Count(); k++)
                    {
                        if (justMVPNick[k].Length > 2)
                        {
                            MVPName = justMVPNick[k];
                            break;
                        }
                    }
                }
                double MVPHSR = Math.Round(matchResultInfo.MVPlayer.HSR, 2);
                double hsr = Math.Round(MVPHSR * 100); // ebala
                // DRAWING TEAM NAMES
                Drawing.Tools.DrawText(graphics, matchResultInfo.MatchScore.AName, new Rectangle(300, 300, 0, 200), StringAlignment.Center, 30, Brushes.White, FontStyle.Regular, "Play-Regular");
                Drawing.Tools.DrawText(graphics, matchResultInfo.MatchScore.BName, new Rectangle(990, 300, 0, 200), StringAlignment.Center, 30, Brushes.White, FontStyle.Regular, "Play-Regular");
                // DRAWING TEAM SCORES
                Drawing.Tools.DrawText(graphics, matchResultInfo.MatchScore.AScore, new Rectangle(300, 225, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
                Drawing.Tools.DrawText(graphics, matchResultInfo.MatchScore.BScore, new Rectangle(985, 225, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
                // DRAWING MAP NAME 
                Drawing.Tools.DrawText(graphics, CutMapName(matchResultInfo.MapName), new Rectangle(650, 225, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
                // DRAWING MVP PLAYER NAME 
                Drawing.Tools.Draw(graphics, MVPName, 420, 403, 36);
                // DRAWING FIRST BLOCK
                Drawing.Tools.DrawText(graphics, matchResultInfo.MVPlayer.WinRate.ToString() + "%", new Rectangle(285, 540, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
                Drawing.Tools.DrawText(graphics, matchResultInfo.MVPlayer.TotalPlayed.ToString(), new Rectangle(400, 640, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                Drawing.Tools.DrawText(graphics, matchResultInfo.MVPlayer.Won.ToString(), new Rectangle(400, 690, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                Drawing.Tools.DrawText(graphics, matchResultInfo.MVPlayer.Lost.ToString(), new Rectangle(400, 738, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                // DRAWING SECOND BLOCK
                Drawing.Tools.DrawText(graphics, hsr.ToString() + "%", new Rectangle(670, 540, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
                Drawing.Tools.DrawText(graphics, matchResultInfo.MVPlayer.KDR.ToString(), new Rectangle(795, 640, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                Drawing.Tools.DrawText(graphics, matchResultInfo.MVPlayer.AVG.ToString(), new Rectangle(795, 690, 0, 200), StringAlignment.Far, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                // DRAWING THIRD BLOCK
                graphics.DrawImage(Drawing.Tools.GetRankImage(matchResultInfo.MVPlayer.RankName), 875, 540, 60, 60);
                Drawing.Tools.DrawText(graphics, matchResultInfo.MVPlayer.RankMMR.ToString(), new Rectangle(1050, 540, 0, 200), StringAlignment.Center, 36, Brushes.White, FontStyle.Regular, "Play-Regular");
                // DRAWING SCOREBOARD
                int yForNames = 910;
                int yForNumbers = 910;

                IEnumerable<PlayerResult> FirstTeam = matchResultInfo.PlayerResults.Where(x => x.TeamName == matchResultInfo.MatchScore.AName);
                IEnumerable<PlayerResult> SecondTeam = matchResultInfo.PlayerResults.Where(x => x.TeamName == matchResultInfo.MatchScore.BName);

                for (int i = 0; i < FirstTeam.Count(); i++)
                {
                    if (i > 4) break;

                    string name = FirstTeam.ElementAt(i).Name;
                    var justNick = FirstTeam.ElementAt(i).Name.Split(' ');
                    if (justNick.Count() > 1)
                    {
                        for (int k = 1; k < justNick.Count(); k++)
                        {
                            if (justNick[k].Length > 2)
                            {
                                name = justNick[k];
                                break;
                            }
                        }
                    }

                    name = name.Length > 17 ? name[..17] : name;

                    Drawing.Tools.Draw(graphics, name, 100, yForNames, 16);
                    Drawing.Tools.DrawText(graphics, FirstTeam.ElementAt(i).Kills.ToString(), new Rectangle(423, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                    Drawing.Tools.DrawText(graphics, FirstTeam.ElementAt(i).Deaths.ToString(), new Rectangle(505, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                    Drawing.Tools.DrawText(graphics, FirstTeam.ElementAt(i).KDR.ToString(), new Rectangle(585, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                    yForNames += 57;
                    yForNumbers += 56;
                }

                yForNames = 910;
                yForNumbers = 910;

                for (int i = 0; i < SecondTeam.Count(); i++)
                {
                    if (i > 4) break;

                    string name = SecondTeam.ElementAt(i).Name;
                    var justNick = SecondTeam.ElementAt(i).Name.Split(' ');
                    if (justNick.Count() > 1)
                    {
                        for (int k = 1; k < justNick.Count(); k++)
                        {
                            if (justNick[k].Length > 2)
                            {
                                name = justNick[k];
                                break;
                            }
                        }
                    }

                    name = name.Length > 17 ? name[..17] : name;

                    Drawing.Tools.Draw(graphics, name, 660, yForNames, 16);
                    Drawing.Tools.DrawText(graphics, SecondTeam.ElementAt(i).Kills.ToString(), new Rectangle(985, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                    Drawing.Tools.DrawText(graphics, SecondTeam.ElementAt(i).Deaths.ToString(), new Rectangle(1067, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                    Drawing.Tools.DrawText(graphics, SecondTeam.ElementAt(i).KDR.ToString(), new Rectangle(1147, yForNumbers, 0, 200), StringAlignment.Center, 18, Brushes.White, FontStyle.Regular, "Play-Regular");
                    yForNames += 57;
                    yForNumbers += 56;
                }

                string uploadImage = DateTime.Now.ToString("yyyy-MM-dd_hh_mm_ss") + ".png";

                image.Save(uploadImage, System.Drawing.Imaging.ImageFormat.Png);

                var web = new WebClient();
                var api = new VkApi();
                api.Authorize(new ApiAuthParams
                {
                    AccessToken = Program.ConfigTools.Config.VKToken,
                });

                var wallPostParams = new WallPostParams
                {
                    OwnerId = -Program.ConfigTools.Config.StatGroupID,
                    Message = $"Подробнее: https://ktvcss.ru/match/{matchId}",
                    FromGroup = true,
                    Signed = false
                };
                var uploadServer = api.Photo.GetWallUploadServer(Program.ConfigTools.Config.StatGroupID);
                var result = Encoding.ASCII.GetString(web.UploadFile(uploadServer.UploadUrl, uploadImage));
                var photo = api.Photo.SaveWallPhoto(result, (ulong?)Program.ConfigTools.Config.AdminVkID, (ulong?)Program.ConfigTools.Config.StatGroupID);
                wallPostParams.Attachments = new List<MediaAttachment>
                    {
                        photo.FirstOrDefault()
                    };
                api.Wall.Post(wallPostParams);
                try
                {
                    File.Delete(uploadImage);
                }
                catch (Exception ex)
                {
                    Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Print(Program.Node.ServerID, $"[Message] {ex.Message} [StackTrace] {ex.StackTrace} [InnerException] {ex.InnerException}", LogLevel.Error);
            }
        }
    }
}
