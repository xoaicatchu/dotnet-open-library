using EnterpriseClassic.Api.Features.Orders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseClassic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    public OrdersController(IMediator mediator) => _mediator = mediator;
    
    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetAll() =>
        Ok(await _mediator.Send(new GetOrdersQuery()));
    
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _mediator.Send(new CreateOrderCommand(request));
        return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
    }
}
