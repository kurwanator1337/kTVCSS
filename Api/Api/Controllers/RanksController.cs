using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Api.Controllers
{
    /// <summary>
    /// Ranks
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RanksController : ControllerBase
    {
        /// <summary>
        /// Get rank list
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public object Get()
        {
            using (SqlConnection connection = new SqlConnection(Program.SQLConnectionString))
            {
                connection.Open();
                SqlCommand query = new SqlCommand($"SELECT * FROM [kTVCSS].[dbo].[Ranks]", connection);
                using (var reader = query.ExecuteReader())
                {
                    DataTable dataTable = new DataTable();
                    dataTable.Load(reader);
                    return Tools.DataTableToList<Ranks>(dataTable);
                }
            }
        }
    }

    public class Ranks
    {
        public string NAME { get; set; }
        public int STARTMMR { get; set; }
        public int ENDMMR { get; set; }
    }
}
