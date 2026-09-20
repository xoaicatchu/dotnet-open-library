using Microsoft.AspNetCore.Mvc;
using ExcelReportManager.Api.Models;
using ExcelReportManager.Api.Services;

namespace ExcelReportManager.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ExcelReportService _reportService;

    public ReportsController(ExcelReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Exports a list of sales records as a styled Excel workbook (.xlsx).
    /// </summary>
    [HttpPost("export")]
    public IActionResult Export([FromBody] List<SaleRecordDto> records)
    {
        if (records == null || records.Count == 0)
            return BadRequest(new { Error = "At least one sale record is required for export" });

        var bytes = _reportService.ExportSalesReport(records);
        var fileName = $"SalesReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Imports sales records from an uploaded Excel file (.xlsx).
    /// </summary>
    [HttpPost("import")]
    public ActionResult<ExcelImportResultDto> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Error = "A non-empty file is required" });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { Error = "Only .xlsx files are supported" });

        using var stream = file.OpenReadStream();
        var result = _reportService.ImportSalesReport(stream);

        return Ok(result);
    }

    /// <summary>
    /// Returns sample sales data for testing.
    /// </summary>
    [HttpGet("sample-data")]
    public ActionResult<List<SaleRecordDto>> GetSampleData()
    {
        var data = new List<SaleRecordDto>
        {
            new("TXN-1001", "Acme Corporation", "Enterprise Cloud License", 5, 1200.00m, new DateTime(2026, 1, 15)),
            new("TXN-1002", "Global Tech Ltd", "Database Migration Suite", 2, 4500.00m, new DateTime(2026, 2, 1)),
            new("TXN-1003", "Starlight Media", "API Gateway Pro", 10, 300.00m, new DateTime(2026, 2, 20)),
            new("TXN-1004", "VNPT IT Solutions", "Security Audit Service", 1, 8000.00m, new DateTime(2026, 3, 5))
        };

        return Ok(data);
    }
}
