using Api.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Controllers
{
    public class MatchesList
    {
        public int ID { get; set; }
    }
    /// <summary>
    /// Matches
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        /// <summary>
        /// Get all matches
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public object Get()
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT ID FROM [kTVCSS].[dbo].[Matches]", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<MatchesList>(dataTable);
                }
            }
        }
        /// <summary>
        /// Get match by id
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id}")]
        public object Get(int id)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[Matches] WHERE ID = {id}", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<Matches>(dataTable);
                }
            }
        }

        /// <summary>
        /// Get match demoname
        /// </summary>
        /// <param name="id">match id</param>
        /// <returns></returns>
        [HttpGet("demo/{id}")]
        public string GetMatchDemo(int id)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT DEMONAME FROM [kTVCSS].[dbo].[MatchesDemos] WHERE ID = {id}", connection);
                using (var reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader[0].ToString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get MVP of the match
        /// </summary>
        /// <param name="id">match id</param>
        /// <returns></returns>
        [HttpGet("mvp/{id}")]
        public string GetMatchMVP(int id)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT MVP FROM [kTVCSS].[dbo].[MatchesMVP] WHERE ID = {id}", connection);
                using (var reader = query.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader[0].ToString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get match highlights
        /// </summary>
        /// <param name="id">match id</param>
        /// <returns></returns>
        [HttpGet("highlights/{id}")]
        public List<MatchesHighlights> GetMatchesHighlights(int id)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[MatchesHighlights] WHERE ID = {id}", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<MatchesHighlights>(dataTable);
                }
            }
        }
        /// <summary>
        /// Get match log
        /// </summary>
        /// <param name="id">match id</param>
        /// <returns></returns>
        [HttpGet("logs/{id}")]
        public List<MatchesLogs> GetMatchesLogs(int id)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[MatchesLogs] WHERE MATCHID = {id} ORDER BY DATETIME ASC", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<MatchesLogs>(dataTable);
                }
            }
        }

        /// <summary>
        /// Get match results
        /// </summary>
        /// <param name="id">match id</param>
        /// <returns></returns>
        [HttpGet("results/{id}")]
        public List<MatchesResults> GetMatchesResults(int id)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[MatchesResults] WHERE ID = {id}", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<MatchesResults>(dataTable);
                }
            }
        }
        
        /// <summary>
        /// Get live matches
        /// </summary>
        /// <returns></returns>
        [HttpGet("live")]
        public List<MatchesLive> GetMatchesLive()
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[MatchesLive] WHERE FINISHED = 0", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<MatchesLive>(dataTable);
                }
            }
        }

        /// <summary>
        /// Get live results of the match
        /// </summary>
        /// <param name="id">match id</param>
        /// <returns></returns>
        [HttpGet("liveresults/{id}")]
        public List<MatchesResultsLive> GetMatchesResultsLive(int id)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[MatchesResultsLive] WHERE ID = {id}", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<MatchesResultsLive>(dataTable);
                }
            }
        }

        /// <summary>
        /// Get match backup info
        /// </summary>
        /// <param name="id">match id</param>
        /// <returns></returns>
        [HttpGet("backups/{id}")]
        public List<MatchesBackups> GetMatchesBackups(int id)
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[MatchesBackups] WHERE ID = {id}", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<MatchesBackups>(dataTable);
                }
            }
        }
    }
}
