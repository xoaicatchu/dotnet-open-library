using Microsoft.AspNetCore.Mvc;
using CsvDataProcessor.Api.Models;
using CsvDataProcessor.Api.Services;

namespace CsvDataProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CsvController : ControllerBase
{
    private readonly CsvService _csvService;

    public CsvController(CsvService csvService)
    {
        _csvService = csvService;
    }

    /// <summary>
    /// Returns sample customer data.
    /// </summary>
    [HttpGet("sample")]
    public ActionResult<List<CustomerRecord>> GetSample()
    {
        var sample = new List<CustomerRecord>
        {
            new()
            {
                Id = "CUST-001",
                FullName = "Nguyen Van A",
                Email = "anguyen@example.com",
                PhoneNumber = "+84-901-234-567",
                Balance = 1500.50m,
                IsActive = true,
                RegisteredDate = new DateTime(2025, 6, 15)
            },
            new()
            {
                Id = "CUST-002",
                FullName = "Tran Thi B",
                Email = "btran@example.com",
                PhoneNumber = "+84-912-345-678",
                Balance = 420.00m,
                IsActive = false,
                RegisteredDate = new DateTime(2025, 9, 20)
            },
            new()
            {
                Id = "CUST-003",
                FullName = "Le Van C",
                Email = "cle@example.com",
                PhoneNumber = "+84-988-776-655",
                Balance = 8900.75m,
                IsActive = true,
                RegisteredDate = new DateTime(2026, 1, 10)
            }
        };

        return Ok(sample);
    }

    /// <summary>
    /// Exports a list of customers to a downloadable CSV file.
    /// </summary>
    [HttpPost("export")]
    public IActionResult Export([FromBody] List<CustomerRecord> customers)
    {
        if (customers == null || customers.Count == 0)
            return BadRequest(new { Error = "At least one customer record is required for export" });

        var bytes = _csvService.ExportCustomersCsv(customers);
        var fileName = $"Customers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// Imports customer records from an uploaded CSV file.
    /// </summary>
    [HttpPost("import")]
    public async Task<ActionResult<CsvImportResultDto>> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Error = "A non-empty CSV file is required" });

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { Error = "Only .csv files are supported" });

        using var stream = file.OpenReadStream();
        var result = await _csvService.ImportCustomersCsvAsync(stream);

        return Ok(result);
    }
}
