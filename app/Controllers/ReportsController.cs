using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace app.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly SqlConnection _connection;

        public ReportsController(SqlConnection connection)
        {
            _connection = connection;
        }

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] string metric = "profit")
        {
            string query;
            string orderBy;

            switch (metric.ToLower())
            {

                case "profit":
                default:
                    query = @"
                        SELECT 
                            S.ServiceName,
                            (SUM(I.TotalAmount) - SUM(P.BaseSalary + P.OvertimePay - P.Deductions)) AS MetricValue
                        FROM FactInvoice I
                        JOIN DimService S ON I.DimServiceServiceID = S.ServiceID
                        JOIN FactServiceAssignment A ON A.DimServiceServiceID = S.ServiceID
                        JOIN FactPayroll P ON P.DimEmployeeEmployeeID = A.DimEmployeeEmployeeID
                        GROUP BY S.ServiceName
                        ORDER BY MetricValue DESC;";
                    break;
            }

            var results = new List<object>();

            await _connection.OpenAsync();
            using (var cmd = new SqlCommand(query, _connection))
            {
                var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        ServiceName = reader["ServiceName"].ToString(),
                        MetricValue = reader["MetricValue"] == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(reader["MetricValue"])
                    });
                }
            }
            await _connection.CloseAsync();

            return Ok(results);
        }
    }
}
