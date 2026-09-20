using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Api.Data;
using PaymentService.Api.Events;
using PaymentService.Api.Models;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly PaymentStore _store;
    private readonly ICapPublisher _publisher;

    public PaymentsController(PaymentStore store, ICapPublisher publisher)
    {
        _store = store;
        _publisher = publisher;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.OrderId)) return BadRequest("OrderId is required");
        if (req.Amount <= 0) return BadRequest("Amount must be greater than 0");
        if (string.IsNullOrWhiteSpace(req.Currency) || req.Currency.Length != 3) return BadRequest("Currency must be exactly 3 characters");

        var payment = _store.Add(req.OrderId, req.Amount, req.Currency);
        
        await _publisher.PublishAsync("payment.created", new PaymentCreatedEvent(payment.Id, payment.OrderId, payment.Amount, payment.Currency));
        
        return Created($"/api/payments/{payment.Id}", PaymentResponse.From(payment));
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var payments = _store.GetAll().Select(PaymentResponse.From);
        return Ok(payments);
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var payment = _store.GetById(id);
        if (payment == null) return NotFound();
        return Ok(PaymentResponse.From(payment));
    }

    [HttpPost("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, [FromBody] RefundPaymentRequest req)
    {
        var payment = _store.GetById(id);
        if (payment == null) return NotFound();

        await _publisher.PublishAsync("payment.refunded", new PaymentRefundedEvent(payment.Id, req.Reason));
        
        return Ok(new { Message = "Refund initiated" });
    }
}
