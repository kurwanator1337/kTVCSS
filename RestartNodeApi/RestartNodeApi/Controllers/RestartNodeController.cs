using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Http.Json;

namespace RestartNodeApi.Controllers
{
    public class MishaPidor
    {
        public int Value { get; set; }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class RestartNodeController : ControllerBase
    {
        [HttpGet("{id}")]
        public object Get(int id)
        {
            int isBusy = 0;
            using (SqlConnection connection = new SqlConnection("Data Source=localhost;Initial Catalog=kTVCSS;Integrated Security=SSPI;"))
            {
                connection.Open();
                using (SqlCommand query = new SqlCommand($"SELECT BUSY FROM GameServers WHERE ID = {id}", connection))
                {
                    isBusy = int.Parse(query.ExecuteScalar().ToString());
                }
            }
            if (isBusy == 1)
            {
                MishaPidor misha = new MishaPidor() { Value = 1 }; // KPR нельзя перезапустить, т.к. на этом сервере находится активный матч!
                return new JsonResult(misha);
            }
            else
            {
                Process[] processes = Process.GetProcessesByName($"kTVCSS{id}");
                foreach (Process process in processes)
                {
                    process.Kill();
                }
                MishaPidor misha = new MishaPidor() { Value = 0 }; ; // сукесс
                return new JsonResult(misha);
            }
        }
    }
}
