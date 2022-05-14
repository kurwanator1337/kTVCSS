using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Controllers
{
    /// <summary>
    /// Maps
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MapsController : ControllerBase
    {
        /// <summary>
        /// Get current project's map pool
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public object Get()
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT MAP FROM [kTVCSS].[dbo].[MapPool]", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<Map>(dataTable);
                }
            }
        }
    }

    public class Map { public string MAP { get; set; } }
}
