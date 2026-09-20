using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MassTransit;
using SagaPattern.Api.Models;
using SagaPattern.Api.Messages;

namespace SagaPattern.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    
    public OrdersController(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    [HttpPost]
    public async Task<ActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var orderId = Guid.NewGuid();
        await _publishEndpoint.Publish(new OrderPlaced(orderId, request.CustomerName, request.TotalAmount));
        return Accepted(new { OrderId = orderId, Message = "Order placed, saga started" });
    }
}
