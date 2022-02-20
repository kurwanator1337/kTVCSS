﻿using kTVCSS.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kTVCSS.Tools
{
    public static class OnPlayerJoinTheServer
    {
        public static async Task<ConnectInfo> PrintPlayerInfo(string steamID)
        {
            using (SqlConnection connection = new SqlConnection(Program.ConfigTools.Config.SQLConnectionString))
            {
                await connection.OpenAsync();
                SqlCommand query = new SqlCommand($"SELECT KDR, HSR, MMR, AVG, RANKNAME, WINRATE, ISCALIBRATION, MATCHESPLAYED FROM [dbo].[Players] WHERE STEAMID = '{steamID}'", connection);
                using (var reader = await query.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ConnectInfo info = new ConnectInfo();

                        double.TryParse(reader[0].ToString(), out double kdr);
                        double.TryParse(reader[1].ToString(), out double hsr);
                        int.TryParse(reader[2].ToString(), out int mmr);
                        double.TryParse(reader[3].ToString(), out double avg);
                        double.TryParse(reader[5].ToString(), out double winrate);
                        int.TryParse(reader[6].ToString(), out int isCalibration);
                        int.TryParse(reader[7].ToString(), out int matchesPlayed);

                        info.KDR = kdr;
                        info.HSR = hsr;
                        info.MMR = mmr;
                        info.AVG = avg;
                        info.WinRate = winrate;
                        info.IsCalibration = isCalibration;
                        info.RankName = reader[4].ToString();
                        info.MatchesPlayed = matchesPlayed;

                        return info;
                    }
                }
            }
            return null;
        }
    }
}