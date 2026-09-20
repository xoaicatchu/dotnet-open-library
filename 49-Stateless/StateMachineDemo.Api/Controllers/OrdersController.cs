using Microsoft.AspNetCore.Mvc;
using StateMachineDemo.Api.Models;
using StateMachineDemo.Api.Services;

namespace StateMachineDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderStateMachineService _stateMachineService;

    public OrdersController(OrderStateMachineService stateMachineService)
    {
        _stateMachineService = stateMachineService;
    }

    /// <summary>
    /// Creates a new order in Draft state.
    /// </summary>
    [HttpPost]
    public ActionResult<OrderDto> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(new { Error = "CustomerName is required" });

        if (request.TotalAmount <= 0)
            return BadRequest(new { Error = "TotalAmount must be positive" });

        var order = _stateMachineService.CreateOrder(request.CustomerName, request.TotalAmount);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    /// <summary>
    /// Gets order details, current state, permitted triggers, and history.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<OrderDto> GetOrder(string id)
    {
        var order = _stateMachineService.GetOrder(id);
        if (order == null)
            return NotFound(new { Error = "Order not found" });

        return Ok(order);
    }

    /// <summary>
    /// Fires a transition trigger on the order's state machine.
    /// </summary>
    [HttpPost("{id}/fire")]
    public ActionResult<OrderDto> FireTrigger(string id, [FromBody] FireTriggerRequest request)
    {
        var (success, error, order) = _stateMachineService.FireTrigger(id, request.Trigger, request.Reason);

        if (!success)
            return BadRequest(new { Error = error });

        return Ok(order);
    }

    /// <summary>
    /// Returns visual diagram of the state machine (DOT or Mermaid format).
    /// </summary>
    [HttpGet("{id}/graph")]
    public ActionResult<string> GetGraph(string id, [FromQuery] string format = "dot")
    {
        var graph = _stateMachineService.GetGraph(id, format);
        if (graph == null)
            return NotFound(new { Error = "Order not found" });

        var contentType = format.Equals("mermaid", StringComparison.OrdinalIgnoreCase)
            ? "text/plain"
            : "text/vnd.graphviz";

        return Content(graph, contentType);
    }
}
