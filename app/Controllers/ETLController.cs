using Microsoft.AspNetCore.Mvc;
using System.Data.Odbc;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class ETLController : ControllerBase
{
    private readonly ETLService _etlService;

    public ETLController(ETLService etlService)
    {
        _etlService = etlService ?? throw new ArgumentNullException(nameof(etlService));
    }

    [HttpGet("run")]
    public async Task<IActionResult> RunETL()
    {
        var log = new StringBuilder();
        log.AppendLine("Starting ETL...");

        try
        {
            await _etlService.RunETLAsync(log);
            log.AppendLine("ETL job completed successfully!");
        }
        catch (Exception ex)
        {
            log.AppendLine($"ETL failed: {ex.Message}");
        }

        return Content(log.ToString(), "text/plain");
    }
    
    [HttpGet("purge")]
    public async Task<IActionResult> RunETLClear()
    {
        var log = new StringBuilder();
        log.AppendLine("Clearing...");

        try
        {
            await _etlService.ClearETLAsync(log);
            log.AppendLine("Clear completed successfully!");
        }
        catch (Exception ex)
        {
            log.AppendLine($"Clear failed: {ex.Message}");
        }

        return Content(log.ToString(), "text/plain");
    }
}