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

        // 4 human queries expressed in sql
        // retention  -> YoY client retention % (overall and per service)
        // staffing   -> Monthly demand vs staffing capacity (8h per shift)
        // profit     -> Profit by service (revenue - allocated payroll)
        // damages    -> Damage report hotspots by asset type/location
        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] string metric = "profit")
        {
            string query;

            switch (metric.ToLower())
            {
                case "retention":
                    // Year Y clients who also register in Y+1.
                    query = @"
;WITH R AS (
    SELECT
        YEAR(RegistrationDate) AS Yr,
        DimClientClientID AS ClientID,
        DimServiceServiceID AS ServiceID
    FROM FactServiceRegistration
),
PerService AS (
    SELECT
        r1.Yr,
        s.ServiceName,
        COUNT(DISTINCT r1.ClientID) AS TotalClients,
        COUNT(DISTINCT CASE WHEN EXISTS (
            SELECT 1 FROM R r2
            WHERE r2.ClientID = r1.ClientID
              AND r2.Yr = r1.Yr + 1
              AND r2.ServiceID = r1.ServiceID
        ) THEN r1.ClientID END) AS RetainedClients
    FROM R r1
    JOIN DimService s ON s.ServiceID = r1.ServiceID
    GROUP BY r1.Yr, s.ServiceName
),
Overall AS (
    SELECT
        r1.Yr,
        'All Services' AS ServiceName,
        COUNT(DISTINCT r1.ClientID) AS TotalClients,
        COUNT(DISTINCT CASE WHEN EXISTS (
            SELECT 1 FROM R r2
            WHERE r2.ClientID = r1.ClientID
              AND r2.Yr = r1.Yr + 1
        ) THEN r1.ClientID END) AS RetainedClients
    FROM R r1
    GROUP BY r1.Yr
)
SELECT
    Yr AS [Year],
    ServiceName,
    TotalClients,
    RetainedClients,
    CASE WHEN TotalClients = 0 THEN 0
         ELSE RetainedClients * 100.0 / TotalClients END AS MetricValue
FROM (
    SELECT * FROM PerService
    UNION ALL
    SELECT * FROM Overall
) x
ORDER BY [Year], ServiceName;
";
                    break;

                case "staffing":
                    // Demand (assignments) vs capacity (approx 8h per shift).
                    query = @"
;WITH Demand AS (
    SELECT
        DATEFROMPARTS(YEAR(sa.ScheduledDate), MONTH(sa.ScheduledDate), 1) AS MonthStart,
        s.ServiceName,
        COUNT(*) AS TotalScheduledServices
    FROM FactServiceAssignment sa
    JOIN DimService s ON s.ServiceID = sa.DimServiceServiceID
    GROUP BY DATEFROMPARTS(YEAR(sa.ScheduledDate), MONTH(sa.ScheduledDate), 1), s.ServiceName
),
Capacity AS (
    SELECT
        DATEFROMPARTS(YEAR(sh.StartTime), MONTH(sh.StartTime), 1) AS MonthStart,
        COUNT(*) * 8.0 AS TotalShiftHours  -- 8 hours per shift assumption
    FROM FactShifts sh
    GROUP BY DATEFROMPARTS(YEAR(sh.StartTime), MONTH(sh.StartTime), 1)
)
SELECT
    FORMAT(d.MonthStart, 'yyyy-MM') AS [Month],
    d.ServiceName,
    d.TotalScheduledServices,
    ISNULL(c.TotalShiftHours, 0) AS TotalShiftHours,
    CASE WHEN d.TotalScheduledServices = 0 THEN NULL
         ELSE (ISNULL(c.TotalShiftHours, 0) * 1.0) / d.TotalScheduledServices END AS MetricValue
FROM Demand d
LEFT JOIN Capacity c ON c.MonthStart = d.MonthStart
ORDER BY [Month], d.ServiceName;
";
                    break;

                case "damages":
                    query = @"
SELECT
    a.AssetType,
    a.Location,
    COUNT(dr.ReportID) AS TotalReports,
    AVG(CAST(dr.RepairCost AS DECIMAL(18,2))) AS AvgRepairCost,
    SUM(CAST(dr.RepairCost AS DECIMAL(18,2))) AS MetricValue
FROM FactDamageReport dr
JOIN DimAsset a ON a.AssetID = dr.DimAssetAssetID
GROUP BY a.AssetType, a.Location
ORDER BY MetricValue DESC, TotalReports DESC;
";
                    break;

                case "profit":
                default:
                    // Revenue by service from FactInvoice.
                    // Payroll allocation: for each employee, allocate their total payroll to services
                    // in proportion to their assignment counts by service.
                    query = @"
;WITH Revenue AS (
    SELECT
        s.ServiceID,
        s.ServiceName,
        SUM(CAST(i.TotalAmount AS DECIMAL(18,2))) AS Revenue
    FROM FactInvoice i
    JOIN DimService s ON s.ServiceID = i.DimServiceServiceID
    GROUP BY s.ServiceID, s.ServiceName
),
EmpPayroll AS (
    SELECT
        p.DimEmployeeEmployeeID AS EmployeeID,
        SUM(CAST((p.BaseSalary + p.OverTimePay - p.Deductions) AS DECIMAL(18,2))) AS PayrollCost
    FROM FactPayroll p
    GROUP BY p.DimEmployeeEmployeeID
),
EmpAssign AS (
    SELECT
        sa.DimEmployeeEmployeeID AS EmployeeID,
        sa.DimServiceServiceID AS ServiceID,
        COUNT(*) AS AssignCount
    FROM FactServiceAssignment sa
    GROUP BY sa.DimEmployeeEmployeeID, sa.DimServiceServiceID
),
EmpAssignTotals AS (
    SELECT
        EmployeeID,
        SUM(AssignCount) AS TotalAssigns
    FROM EmpAssign
    GROUP BY EmployeeID
),
AllocatedCost AS (
    SELECT
        ea.ServiceID,
        SUM( ep.PayrollCost * (ea.AssignCount * 1.0 / NULLIF(eat.TotalAssigns, 0)) ) AS AllocatedPayroll
    FROM EmpAssign ea
    JOIN EmpAssignTotals eat ON eat.EmployeeID = ea.EmployeeID
    JOIN EmpPayroll ep ON ep.EmployeeID = ea.EmployeeID
    GROUP BY ea.ServiceID
)
SELECT
    r.ServiceName,
    r.Revenue,
    ISNULL(ac.AllocatedPayroll, 0) AS AllocatedPayroll,
    (r.Revenue - ISNULL(ac.AllocatedPayroll, 0)) AS MetricValue
FROM Revenue r
LEFT JOIN AllocatedCost ac ON ac.ServiceID = r.ServiceID
ORDER BY MetricValue DESC, r.ServiceName;
";
                    break;
            }

            var results = new List<Dictionary<string, object?>>();

            await _connection.OpenAsync();
            using (var cmd = new SqlCommand(query, _connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var val = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[name] = val;
                    }
                    results.Add(row);
                }
            }
            await _connection.CloseAsync();

            return Ok(results);
        }

        // Simple export that works best for single-value metrics per dimension (e.g., profit by service).
        // You can hit: /api/reports/export?metric=profit
        [HttpGet("export")]
        public async Task<IActionResult> ExportAnalytics([FromQuery] string metric = "profit")
        {
            ExcelPackage.License.SetNonCommercialPersonal("Eric");

            // For export, we’ll run the same query path, then dump whatever columns come back.
            var queryForExportMetric = metric?.ToLower() switch
            {
                "retention" => @"
;WITH R AS (
    SELECT YEAR(RegistrationDate) AS Yr, DimClientClientID AS ClientID, DimServiceServiceID AS ServiceID
    FROM FactServiceRegistration
),
PerService AS (
    SELECT r1.Yr, s.ServiceName,
           COUNT(DISTINCT r1.ClientID) AS TotalClients,
           COUNT(DISTINCT CASE WHEN EXISTS (
                SELECT 1 FROM R r2 WHERE r2.ClientID = r1.ClientID AND r2.Yr = r1.Yr + 1 AND r2.ServiceID = r1.ServiceID
           ) THEN r1.ClientID END) AS RetainedClients
    FROM R r1
    JOIN DimService s ON s.ServiceID = r1.ServiceID
    GROUP BY r1.Yr, s.ServiceName
),
Overall AS (
    SELECT r1.Yr, 'All Services' AS ServiceName,
           COUNT(DISTINCT r1.ClientID) AS TotalClients,
           COUNT(DISTINCT CASE WHEN EXISTS (
                SELECT 1 FROM R r2 WHERE r2.ClientID = r1.ClientID AND r2.Yr = r1.Yr + 1
           ) THEN r1.ClientID END) AS RetainedClients
    FROM R r1
    GROUP BY r1.Yr
)
SELECT
    Yr AS [Year],
    ServiceName,
    TotalClients,
    RetainedClients,
    CASE WHEN TotalClients = 0 THEN 0 ELSE RetainedClients * 100.0 / TotalClients END AS MetricValue
FROM (SELECT * FROM PerService UNION ALL SELECT * FROM Overall) x
ORDER BY [Year], ServiceName;",
                "staffing" => @"
;WITH Demand AS (
    SELECT DATEFROMPARTS(YEAR(sa.ScheduledDate), MONTH(sa.ScheduledDate), 1) AS MonthStart,
           s.ServiceName,
           COUNT(*) AS TotalScheduledServices
    FROM FactServiceAssignment sa
    JOIN DimService s ON s.ServiceID = sa.DimServiceServiceID
    GROUP BY DATEFROMPARTS(YEAR(sa.ScheduledDate), MONTH(sa.ScheduledDate), 1), s.ServiceName
),
Capacity AS (
    SELECT DATEFROMPARTS(YEAR(sh.StartTime), MONTH(sh.StartTime), 1) AS MonthStart,
           COUNT(*) * 8.0 AS TotalShiftHours
    FROM FactShifts sh
    GROUP BY DATEFROMPARTS(YEAR(sh.StartTime), MONTH(sh.StartTime), 1)
)
SELECT FORMAT(d.MonthStart, 'yyyy-MM') AS [Month],
       d.ServiceName,
       d.TotalScheduledServices,
       ISNULL(c.TotalShiftHours, 0) AS TotalShiftHours,
       CASE WHEN d.TotalScheduledServices = 0 THEN NULL
            ELSE (ISNULL(c.TotalShiftHours, 0) * 1.0) / d.TotalScheduledServices END AS MetricValue
FROM Demand d
LEFT JOIN Capacity c ON c.MonthStart = d.MonthStart
ORDER BY [Month], d.ServiceName;",
                "damages" => @"
SELECT a.AssetType, a.Location,
       COUNT(dr.ReportID) AS TotalReports,
       AVG(CAST(dr.RepairCost AS DECIMAL(18,2))) AS AvgRepairCost,
       SUM(CAST(dr.RepairCost AS DECIMAL(18,2))) AS MetricValue
FROM FactDamageReport dr
JOIN DimAsset a ON a.AssetID = dr.DimAssetAssetID
GROUP BY a.AssetType, a.Location
ORDER BY MetricValue DESC, TotalReports DESC;",
                _ => @"
;WITH Revenue AS (
    SELECT s.ServiceID, s.ServiceName, SUM(CAST(i.TotalAmount AS DECIMAL(18,2))) AS Revenue
    FROM FactInvoice i
    JOIN DimService s ON s.ServiceID = i.DimServiceServiceID
    GROUP BY s.ServiceID, s.ServiceName
),
EmpPayroll AS (
    SELECT p.DimEmployeeEmployeeID AS EmployeeID,
           SUM(CAST((p.BaseSalary + p.OverTimePay - p.Deductions) AS DECIMAL(18,2))) AS PayrollCost
    FROM FactPayroll p
    GROUP BY p.DimEmployeeEmployeeID
),
EmpAssign AS (
    SELECT sa.DimEmployeeEmployeeID AS EmployeeID,
           sa.DimServiceServiceID AS ServiceID,
           COUNT(*) AS AssignCount
    FROM FactServiceAssignment sa
    GROUP BY sa.DimEmployeeEmployeeID, sa.DimServiceServiceID
),
EmpAssignTotals AS (
    SELECT EmployeeID, SUM(AssignCount) AS TotalAssigns
    FROM EmpAssign
    GROUP BY EmployeeID
),
AllocatedCost AS (
    SELECT ea.ServiceID,
           SUM( ep.PayrollCost * (ea.AssignCount * 1.0 / NULLIF(eat.TotalAssigns, 0)) ) AS AllocatedPayroll
    FROM EmpAssign ea
    JOIN EmpAssignTotals eat ON eat.EmployeeID = ea.EmployeeID
    JOIN EmpPayroll ep ON ep.EmployeeID = ea.EmployeeID
    GROUP BY ea.ServiceID
)
SELECT r.ServiceName,
       r.Revenue,
       ISNULL(ac.AllocatedPayroll, 0) AS AllocatedPayroll,
       (r.Revenue - ISNULL(ac.AllocatedPayroll, 0)) AS MetricValue
FROM Revenue r
LEFT JOIN AllocatedCost ac ON ac.ServiceID = r.ServiceID
ORDER BY MetricValue DESC, r.ServiceName;"
            };

            var table = new DataTable();

            await _connection.OpenAsync();
            using (var cmd = new SqlCommand(queryForExportMetric, _connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                table.Load(reader);
            }
            await _connection.CloseAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Analytics");

            // Headers
            for (int c = 0; c < table.Columns.Count; c++)
                ws.Cells[1, c + 1].Value = table.Columns[c].ColumnName;

            // Rows
            for (int r = 0; r < table.Rows.Count; r++)
            {
                for (int c = 0; c < table.Columns.Count; c++)
                    ws.Cells[r + 2, c + 1].Value = table.Rows[r][c];
            }

            // Try to chart last column vs first category column if layout fits
            if (table.Columns.Count >= 2 && table.Rows.Count > 0)
            {
                var chart = ws.Drawings.AddChart("chart", OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered);
                chart.Title.Text = $"{metric} chart";
                chart.SetPosition(table.Rows.Count + 2, 0, 0, 0);
                chart.SetSize(800, 420);
                var dataRange = ws.Cells[2, table.Columns.Count, table.Rows.Count + 1, table.Columns.Count];
                var catRange = ws.Cells[2, 1, table.Rows.Count + 1, 1];
                chart.Series.Add(dataRange, catRange);
            }

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