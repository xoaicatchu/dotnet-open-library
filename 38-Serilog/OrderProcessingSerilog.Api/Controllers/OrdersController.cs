using Microsoft.AspNetCore.Mvc;
using OrderProcessingSerilog.Api.Models;
using Serilog.Context;

namespace OrderProcessingSerilog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private static readonly List<OrderResponse> Orders = [];

    public OrdersController(ILogger<OrdersController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes a new order and records structured log events.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId) || request.Amount <= 0)
        {
            _logger.LogWarning("Invalid order request received: CustomerId={CustomerId}, Amount={Amount}", request.CustomerId, request.Amount);
            return BadRequest(new { message = "CustomerId is required and Amount must be greater than 0." });
        }

        var orderId = $"ORD-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        var order = new OrderResponse(
            OrderId: orderId,
            CustomerId: request.CustomerId,
            Amount: request.Amount,
            ItemName: request.ItemName,
            CreatedAt: DateTime.UtcNow
        );

        Orders.Add(order);

        using (LogContext.PushProperty("CorrelationId", Guid.NewGuid().ToString()))
        {
            _logger.LogInformation("Successfully created order {OrderId} for customer {CustomerId} with amount {Amount:C}",
                order.OrderId, order.CustomerId, order.Amount);
        }

        return CreatedAtAction(nameof(GetById), new { id = order.OrderId }, order);
    }

    /// <summary>
    /// Retrieves order by ID and logs diagnostic context.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string id)
    {
        _logger.LogInformation("Searching for order with ID {OrderId}", id);

        var order = Orders.FirstOrDefault(o => o.OrderId.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found in repository", id);
            return NotFound(new { message = $"Order with ID '{id}' was not found." });
        }

        return Ok(order);
    }

    /// <summary>
    /// Intentionally triggers a payment failure to demonstrate structured error logging with stack trace.
    /// </summary>
    [HttpPost("{id}/fail")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult FailOrder(string id)
    {
        try
        {
            throw new InvalidOperationException($"Payment gateway connection timed out while processing order '{id}'.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete transaction for order {OrderId} due to payment provider timeout", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Payment gateway error", orderId = id });
        }
    }
}
