using Microsoft.AspNetCore.Mvc;
using NServiceBus;
using OrderBilling.Api.Data;
using OrderBilling.Api.Messages;

namespace OrderBilling.Api.Controllers;

public record BillOrderRequest(Guid OrderId, decimal Amount, string CustomerEmail);

[ApiController]
[Route("api/[controller]")]
public class BillingController : ControllerBase
{
    private readonly InvoiceStore _store;
    private readonly IMessageSession _messageSession;

    public BillingController(InvoiceStore store, IMessageSession messageSession)
    {
        _store = store;
        _messageSession = messageSession;
    }

    [HttpPost("bill")]
    public async Task<IActionResult> Bill([FromBody] BillOrderRequest req)
    {
        if (req.Amount <= 0)
            return BadRequest("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(req.CustomerEmail))
            return BadRequest("Customer email is required.");

        await _messageSession.Send(new BillOrderCommand(req.OrderId, req.Amount, req.CustomerEmail));
        
        return Accepted($"/api/billing/invoices/{req.OrderId}", new { req.OrderId, Status = "Processing" });
    }

    [HttpGet("invoices")]
    public IActionResult GetAllInvoices()
    {
        return Ok(_store.GetAll());
    }

    [HttpGet("invoices/{orderId:guid}")]
    public IActionResult GetInvoiceByOrderId(Guid orderId)
    {
        var invoice = _store.GetByOrderId(orderId);
        if (invoice is null)
            return NotFound();

        return Ok(invoice);
    }
}
