using MassTransit;
using Microsoft.AspNetCore.Mvc;
using AiAgentMessaging.Api.Contracts;
using AiAgentMessaging.Api.Data;
using AiAgentMessaging.Api.Models;

namespace AiAgentMessaging.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(OrderStore store, IPublishEndpoint publishEndpoint) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || request.CustomerName.Length > 200)
            return BadRequest("Invalid CustomerName");
        if (string.IsNullOrWhiteSpace(request.Product) || request.Product.Length > 300)
            return BadRequest("Invalid Product");
        if (request.Quantity <= 0)
            return BadRequest("Quantity must be > 0");
        if (request.TotalPrice <= 0)
            return BadRequest("TotalPrice must be > 0");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            Product = request.Product,
            Quantity = request.Quantity,
            TotalPrice = request.TotalPrice,
            Status = "Submitted",
            CreatedAt = DateTime.UtcNow
        };

        store.Add(order);

        await publishEndpoint.Publish(new OrderSubmitted(
            order.Id, order.CustomerName, order.Product, order.Quantity, order.TotalPrice));

        return Accepted($"/api/orders/{order.Id}", order);
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(store.GetAll());

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var order = store.GetById(id);
        return order is not null ? Ok(order) : NotFound();
    }
}
