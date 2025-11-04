using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using OfficeOpenXml;

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
        
        
        
        [HttpGet("export")]
public async Task<IActionResult> ExportAnalytics([FromQuery] string metric = "profit")
{
    ExcelPackage.License.SetNonCommercialPersonal("Bhavnoor");

    // 1. Fetch your analytics data (similar to your GetAnalytics method)
    var data = new List<(string ServiceName, decimal MetricValue)>();
    
    var query = @"
        SELECT S.ServiceName,
               (SUM(I.TotalAmount) - SUM(P.BaseSalary + P.OverTimePay - P.Deductions)) AS MetricValue
        FROM FactInvoice I
        JOIN DimService S ON I.DimServiceServiceID = S.ServiceID
        JOIN FactServiceAssignment A ON A.DimServiceServiceID = S.ServiceID
        JOIN FactPayroll P ON P.DimEmployeeEmployeeID = A.DimEmployeeEmployeeID
        GROUP BY S.ServiceName
        ORDER BY MetricValue DESC;
    ";
    
    await _connection.OpenAsync();
    using (var cmd = new SqlCommand(query, _connection))
    using (var reader = await cmd.ExecuteReaderAsync())
    {
        while (await reader.ReadAsync())
        {
            data.Add((
                ServiceName: reader["ServiceName"].ToString(),
                MetricValue: reader["MetricValue"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MetricValue"])
            ));
        }
    }
    await _connection.CloseAsync();

    // 2. Generate Excel file with EPPlus
    using var package = new OfficeOpenXml.ExcelPackage();
    var sheet = package.Workbook.Worksheets.Add("Analytics");

    // Add headers
    sheet.Cells[1, 1].Value = "Service Name";
    sheet.Cells[1, 2].Value = metric;

    // Add data
    for (int i = 0; i < data.Count; i++)
    {
        sheet.Cells[i + 2, 1].Value = data[i].ServiceName;
        sheet.Cells[i + 2, 2].Value = data[i].MetricValue;
    }

    // 3. Create chart
    var chart = sheet.Drawings.AddChart("chart1", OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered);
    chart.Title.Text = $"{metric} per Service";
    chart.SetPosition(data.Count + 2, 0, 0, 0); // row offset for chart
    chart.SetSize(600, 400);
    var series = chart.Series.Add(sheet.Cells[2, 2, data.Count + 1, 2], sheet.Cells[2, 1, data.Count + 1, 1]);

    // 4. Return as file
    var fileName = $"{metric}-analytics.xlsx";
    var stream = new MemoryStream();
    package.SaveAs(stream);
    stream.Position = 0;

    return File(stream, 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
}

    }
}
