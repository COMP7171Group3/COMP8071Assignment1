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

        [HttpGet("cost-vs-revenue")]
        public async Task<IActionResult> GetCostVsRevenue()
        {
            string query = @"
                SELECT 
                    S.ServiceName,
                    SUM(I.TotalAmount) AS TotalRevenue,
                    SUM(P.BaseSalary + P.OvertimePay - P.Deductions) AS TotalCost,
                    (SUM(I.TotalAmount) - SUM(P.BaseSalary + P.OvertimePay - P.Deductions)) AS Profit
                FROM FactInvoice I
                JOIN DimService S ON I.DimServiceServiceID = S.ServiceID
                JOIN FactServiceAssignment A ON A.DimServiceServiceID = S.ServiceID
                JOIN FactPayroll P ON P.DimEmployeeEmployeeID = A.DimEmployeeEmployeeID
                GROUP BY S.ServiceName
                ORDER BY Profit DESC;
            ";

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
                        TotalRevenue = Convert.ToDecimal(reader["TotalRevenue"]),
                        TotalCost = Convert.ToDecimal(reader["TotalCost"]),
                        Profit = Convert.ToDecimal(reader["Profit"])
                    });
                }
            }
            await _connection.CloseAsync();

            return Ok(results);
        }
    }
}
