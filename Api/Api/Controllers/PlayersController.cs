using Api.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Api
{
    /// <summary>
    /// Get players info
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        public class PlayersList
        {
            public int ID { get; set; }
            public string STEAMID { get; set; }
        }
        /// <summary>
        /// Get all players
        /// </summary>
        [HttpGet]
        public object Get()
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT ID, STEAMID FROM [kTVCSS].[dbo].[Players]", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<PlayersList>(dataTable);
                }
            }
        }

        /// <summary>
        /// Get player by id
        /// </summary>
        /// <param name="id">id in the database</param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public object GetById(int id)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[Players] WHERE ID = {id}", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<Players>(dataTable);
                }
            }
        }
        /// <summary>
        /// Get player by steamid
        /// </summary>
        /// <param name="steamid">player's steamid</param>
        /// <returns></returns>
        [HttpGet("steam/{steamid}")]
        public List<Players> GetBySteamId(string steamid)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[Players] WHERE STEAMID = '{steamid}'", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<Players>(dataTable);
                }
            }
        }
        /// <summary>
        /// Get player's highlights
        /// </summary>
        /// <param name="steamid">player's steamid</param>
        /// <returns></returns>
        [HttpGet("highlights/{steamid}")]
        public List<PlayersHighlights> GetHighlights(string steamid)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[PlayersHighlights] WHERE STEAMID = '{steamid}'", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<PlayersHighlights>(dataTable);
                }
            }
        }

        /// <summary>
        /// Get player's rating progress
        /// </summary>
        /// <param name="steamid">player's steamid</param>
        /// <returns></returns>
        [HttpGet("progress/{steamid}")]
        public List<PlayersRatingProgress> GetProgress(string steamid)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[PlayersRatingProgress] WHERE STEAMID = '{steamid}'", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<PlayersRatingProgress>(dataTable);
                }
            }
        }

        /// <summary>
        /// Get player's weapon kills
        /// </summary>
        /// <param name="steamid">player's steamid</param>
        /// <returns></returns>
        [HttpGet("weapons/{steamid}")]
        public List<PlayersWeaponKills> GetWeaponKills(string steamid)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[PlayersWeaponKills] WHERE STEAMID = '{steamid}'", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<PlayersWeaponKills>(dataTable);
                }
            }
        }

        /// <summary>
        /// Get player's chat history
        /// </summary>
        /// <param name="steamid">player's steamid</param>
        /// <returns></returns>
        [HttpGet("chathistory/{steamid}")]
        public List<ChatHistory> GetChat(string steamid)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[ChatHistory] WHERE STEAMID = '{steamid}'", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<ChatHistory>(dataTable);
                }
            }
        }
    }
}
