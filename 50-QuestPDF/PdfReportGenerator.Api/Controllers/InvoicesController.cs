using Microsoft.AspNetCore.Mvc;
using PdfReportGenerator.Api.Models;
using PdfReportGenerator.Api.Services;

namespace PdfReportGenerator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly PdfGeneratorService _pdfService;

    public InvoicesController(PdfGeneratorService pdfService)
    {
        _pdfService = pdfService;
    }

    /// <summary>
    /// Generates an invoice PDF file download from invoice data.
    /// </summary>
    [HttpPost("generate-pdf")]
    public IActionResult GeneratePdf([FromBody] InvoiceDto invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            return BadRequest(new { Error = "InvoiceNumber is required" });

        if (invoice.Items == null || invoice.Items.Count == 0)
            return BadRequest(new { Error = "At least one invoice item is required" });

        var pdfBytes = _pdfService.GenerateInvoicePdf(invoice);
        var fileName = $"Invoice-{invoice.InvoiceNumber}.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Returns metadata (file size, content type) for the generated PDF without file download.
    /// </summary>
    [HttpPost("metadata")]
    public ActionResult<PdfMetadataDto> GetMetadata([FromBody] InvoiceDto invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            return BadRequest(new { Error = "InvoiceNumber is required" });

        if (invoice.Items == null || invoice.Items.Count == 0)
            return BadRequest(new { Error = "At least one invoice item is required" });

        var metadata = _pdfService.GetInvoiceMetadata(invoice);
        return Ok(metadata);
    }

    /// <summary>
    /// Returns a sample invoice object ready for testing PDF generation.
    /// </summary>
    [HttpGet("sample")]
    public ActionResult<InvoiceDto> GetSample()
    {
        var sample = new InvoiceDto(
            "INV-2026-001",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(30),
            "VNPT Information Technology Company",
            "57 Huynh Thuc Khang, Dong Da, Hanoi, Vietnam",
            "FPT Software Corporation",
            "procurement@fpt-software.com",
            "Duy Tan Street, Cau Giay, Hanoi, Vietnam",
            new List<InvoiceItemDto>
            {
                new("Cloud Server Hosting - 1 Month", 2, 120.00m),
                new("Database Managed Service - Enterprise Tier", 1, 350.00m),
                new("Technical Support & Maintenance (24/7)", 10, 45.00m)
            },
            "Payment is due within 30 days of issue. Bank transfer preferred.");

        return Ok(sample);
    }
}
