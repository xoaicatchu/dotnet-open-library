using Microsoft.AspNetCore.Mvc;
using SnapshotTesting.Api.Models;
using SnapshotTesting.Api.Services;

namespace SnapshotTesting.Api.Controllers;

[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<InvoiceSummary>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var invoices = _invoiceService.GetAllInvoices();
        return Ok(invoices);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById([FromRoute] Guid id)
    {
        var invoice = _invoiceService.GetInvoiceById(id);
        if (invoice == null)
        {
            return NotFound(new { error = $"Invoice with ID '{id}' was not found." });
        }

        return Ok(invoice);
    }

    [HttpGet("{id:guid}/summary")]
    [ProducesResponseType(typeof(InvoiceSummary), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetSummary([FromRoute] Guid id)
    {
        var summary = _invoiceService.GetInvoiceSummary(id);
        if (summary == null)
        {
            return NotFound(new { error = $"Invoice with ID '{id}' was not found." });
        }

        return Ok(summary);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InvoiceDetail), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateInvoiceRequest request)
    {
        if (request.Customer == null || string.IsNullOrWhiteSpace(request.Customer.Name))
        {
            return BadRequest(new { error = "Customer name is required." });
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "At least one invoice line item is required." });
        }

        if (request.Items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0))
        {
            return BadRequest(new { error = "Item quantity must be greater than zero and unit price non-negative." });
        }

        var created = _invoiceService.CreateInvoice(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
