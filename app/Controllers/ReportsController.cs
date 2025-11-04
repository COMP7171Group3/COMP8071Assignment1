using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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

        // 4 human queries expressed in SQL
        // retention  -> YoY client retention % (overall and per service)
        // staffing   -> Monthly demand vs staffing capacity (8h per shift)
        // profit     -> Profit by service (revenue - allocated payroll)
        // damages    -> Damage report hotspots by asset type/location

        [HttpGet("analytics")]
        public async Task<IActionResult> GetAnalytics([FromQuery] string metric = "profit")
        {
            var query = BuildAnalyticsSql(metric);
            var results = new List<Dictionary<string, object?>>();

            try
            {
                await _connection.OpenAsync();

                using var cmd = new SqlCommand(query, _connection);
                using var reader = await cmd.ExecuteReaderAsync();

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
            finally
            {
                if (_connection.State != ConnectionState.Closed)
                    await _connection.CloseAsync();
            }

            return Ok(results);
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportAnalytics([FromQuery] string metric = "profit")
        {
            // EPPlus licensing
            ExcelPackage.License.SetNonCommercialPersonal("Eric");

            var query = BuildAnalyticsSql(metric);
            var table = new DataTable();

            try
            {
                await _connection.OpenAsync();

                using var cmd = new SqlCommand(query, _connection);
                using var reader = await cmd.ExecuteReaderAsync();
                table.Load(reader);
            }
            finally
            {
                if (_connection.State != ConnectionState.Closed)
                    await _connection.CloseAsync();
            }

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Analytics");

            // headers
            for (int c = 0; c < table.Columns.Count; c++)
                ws.Cells[1, c + 1].Value = table.Columns[c].ColumnName;

            // rows
            for (int r = 0; r < table.Rows.Count; r++)
                for (int c = 0; c < table.Columns.Count; c++)
                    ws.Cells[r + 2, c + 1].Value = table.Rows[r][c];

            // simple chart: last column vs first column (if shape fits)
            if (table.Columns.Count >= 2 && table.Rows.Count > 0)
            {
                var chart = ws.Drawings.AddChart("chart1", OfficeOpenXml.Drawing.Chart.eChartType.ColumnClustered);
                chart.Title.Text = $"{metric} chart";
                chart.SetPosition(table.Rows.Count + 2, 0, 0, 0);
                chart.SetSize(800, 420);

                var values = ws.Cells[2, table.Columns.Count, table.Rows.Count + 1, table.Columns.Count];
                var cats = ws.Cells[2, 1, table.Rows.Count + 1, 1];
                chart.Series.Add(values, cats);
            }

            var fileName = $"{metric}-analytics.xlsx";
            var stream = new System.IO.MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
        }

        // Centralized SQL builder so analytics + export always stay in sync
        private static string BuildAnalyticsSql(string metric)
        {
            switch ((metric ?? "profit").ToLower())
            {
                case "collectionrate": // repurposed -> monthly invoice collection rate
                    return @"
;WITH Monthly AS (
    SELECT
        DATEFROMPARTS(YEAR(InvoiceDate), MONTH(InvoiceDate), 1) AS MonthStart,
        SUM(CAST(TotalAmount AS DECIMAL(18,2)))                                         AS TotalInvoiced,
        SUM(CASE WHEN IsPaid = 1 THEN CAST(TotalAmount AS DECIMAL(18,2)) ELSE 0 END)   AS TotalPaid,
        COUNT(*)                                                                        AS InvoiceCount,
        SUM(CASE WHEN IsPaid = 1 THEN 1 ELSE 0 END)                                     AS PaidCount
    FROM FactInvoice
    GROUP BY DATEFROMPARTS(YEAR(InvoiceDate), MONTH(InvoiceDate), 1)
)
SELECT
    FORMAT(MonthStart, 'yyyy-MM')                              AS [Month],
    InvoiceCount,
    PaidCount,
    TotalInvoiced,
    TotalPaid,
    CASE WHEN TotalInvoiced = 0 THEN 0
         ELSE (TotalPaid * 100.0 / TotalInvoiced) END          AS MetricValue  -- collection rate %
FROM Monthly
ORDER BY [Month];
";


                case "staffing":
                    // Demand (assignments) vs capacity (approx 8h per shift).
                    return @"
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
ORDER BY [Month], d.ServiceName;";

                case "damages":
                    return @"
SELECT
    a.AssetType,
    a.Location,
    COUNT(dr.ReportID) AS TotalReports,
    AVG(CAST(dr.RepairCost AS DECIMAL(18,2))) AS AvgRepairCost,
    SUM(CAST(dr.RepairCost AS DECIMAL(18,2))) AS MetricValue
FROM FactDamageReport dr
JOIN DimAsset a ON a.AssetID = dr.DimAssetAssetID
GROUP BY a.AssetType, a.Location
ORDER BY MetricValue DESC, TotalReports DESC;";

                case "profit":
                default:
                    // Revenue by service from FactInvoice.
                    // Payroll allocation: for each employee, allocate their total payroll to services
                    // in proportion to their assignment counts by service.
                    return @"
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
ORDER BY MetricValue DESC, r.ServiceName;";
            }
        }
    }
}