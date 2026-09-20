namespace CleanVerticalSlice.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Collections.Generic;
using System.Threading.Tasks;
using CleanVerticalSlice.Application.Orders.GetOrders;
using CleanVerticalSlice.Application.Orders.CreateOrder;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public OrdersController(IMediator mediator) => _mediator = mediator;
    
    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetAll()
        => Ok(await _mediator.Send(new GetOrdersQuery()));
    
    [HttpPost]
    public async Task<ActionResult<OrderSummaryDto>> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _mediator.Send(new CreateOrderCommand(request));
        return Created(string.Empty, result); // Simplified for created at action
    }
}
