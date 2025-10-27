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
                case "retention": // metric value = (retained clients / total clients) * 100
                    query = @"
                        SELECT 
                            YEAR(r1.RegistrationDate) AS Year,
                            s.ServiceName,
                            COUNT(DISTINCT r1.DimClientClientID) AS TotalClients,
                            COUNT(DISTINCT r2.DimClientClientID) AS RetainedClients,
                            (COUNT(DISTINCT r2.DimClientClientID) * 100.0 / COUNT(DISTINCT r1.DimClientClientID)) AS MetricValue
                        FROM FactServiceRegistration r1
                        LEFT JOIN FactServiceRegistration r2
                            ON r1.DimClientClientID = r2.DimClientClientID
                            AND YEAR(r2.RegistrationDate) = YEAR(r1.RegistrationDate) + 1
                        JOIN DimService s ON r1.DimServiceServiceID = s.ServiceID
                        GROUP BY YEAR(r1.RegistrationDate), s.ServiceName
                        ORDER BY Year, s.ServiceName;
                    ";
                    break;

                case "staffing": // metric value = total shift hours / total scheduled services
                    query = @"
                    SELECT 
                        DATENAME(MONTH, sa.ScheduledDate) AS Month,
                        s.ServiceName,
                        e.JobTitle,
                        COUNT(sa.AssignedID) AS TotalScheduledServices,
                        SUM(DATEDIFF(HOUR, sh.StartTime, sh.EndTime)) AS TotalShiftHours,
                        (SUM(DATEDIFF(HOUR, sh.StartTime, sh.EndTime)) * 1.0 /
                            NULLIF(COUNT(sa.AssignedID), 0)) AS MetricValue
                    FROM FactServiceAssignment sa
                    JOIN DimService s ON sa.DimServiceServiceID = s.ServiceID
                    JOIN DimEmployee e ON sa.DimEmployeeEmployeeID = e.EmployeeID
                    LEFT JOIN FactShifts sh ON sa.DimEmployeeEmployeeID = sh.DimEmployeeEmployeeID
                    LEFT JOIN FactPayroll p ON e.EmployeeID = p.DimEmployeeEmployeeID
                    GROUP BY DATENAME(MONTH, sa.ScheduledDate), s.ServiceName, e.JobTitle
                    ORDER BY Month, s.ServiceName, e.JobTitle;
                    ";
                    break;

                case "damages": // metric value = total repair cost
                    query = @"
                        SELECT 
                            a.AssetType,
                            a.Location,
                            COUNT(dr.ReportID) AS TotalReports,
                            AVG(dr.RepairCost) AS AvgRepairCost,
                            SUM(dr.RepairCost) AS MetricValue
                        FROM FactDamageReport dr
                        JOIN DimAsset a ON dr.DimAssetAssetID = a.AssetID
                        GROUP BY a.AssetType, a.Location
                        ORDER BY MetricValue DESC;
                        ";
                    break;

                case "profit": // metric value = total invoice amount - total payroll cost
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
                        ORDER BY MetricValue DESC;
                        ";
                    break;
            }

            var results = new List<object>();

            await _connection.OpenAsync();
            using (var cmd = new SqlCommand(query, _connection))
            {
                var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    switch (metric.ToLower())
                    {
                        case "retention":
                            results.Add(new
                            {
                                Year = reader["Year"],
                                ServiceName = reader["ServiceName"].ToString(),
                                TotalClients = reader["TotalClients"],
                                RetainedClients = reader["RetainedClients"],
                                MetricValue = reader["MetricValue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MetricValue"])
                            });
                            break;

                        case "staffing":
                            results.Add(new
                            {
                                Month = reader["Month"].ToString(),
                                ServiceName = reader["ServiceName"].ToString(),
                                JobTitle = reader["JobTitle"].ToString(),
                                TotalScheduledServices = reader["TotalScheduledServices"],
                                TotalShiftHours = reader["TotalShiftHours"],
                                MetricValue = reader["MetricValue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MetricValue"])
                            });
                            break;

                        case "damages":
                            results.Add(new
                            {
                                AssetType = reader["AssetType"].ToString(),
                                Location = reader["Location"].ToString(),
                                TotalReports = reader["TotalReports"],
                                AvgRepairCost = reader["AvgRepairCost"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["AvgRepairCost"]),
                                MetricValue = reader["MetricValue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MetricValue"])
                            });
                            break;

                        case "profit":
                        default:
                            results.Add(new
                            {
                                ServiceName = reader["ServiceName"].ToString(),
                                MetricValue = reader["MetricValue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MetricValue"])
                            });
                            break;
                    }
                }
            }
            await _connection.CloseAsync();

            return Ok(results);
        }
    }
}
