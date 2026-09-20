using Microsoft.AspNetCore.Mvc;
using Wolverine;
using ModernHighPerf.Api.Features.Orders;

namespace ModernHighPerf.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMessageBus _bus;
    public OrdersController(IMessageBus bus) => _bus = bus;

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetAll()
        => Ok(await _bus.InvokeAsync<List<OrderDto>>(new GetOrdersQuery()));

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _bus.InvokeAsync<OrderDto>(new CreateOrderCommand(request));
        return CreatedAtAction(nameof(GetAll), null, result);
    }
}
