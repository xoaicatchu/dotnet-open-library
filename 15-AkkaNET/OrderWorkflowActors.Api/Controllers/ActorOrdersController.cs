using Akka.Actor;
using Akka.Hosting;
using Microsoft.AspNetCore.Mvc;
using OrderWorkflowActors.Api.Actors;
using OrderWorkflowActors.Api.Messages;

namespace OrderWorkflowActors.Api.Controllers;

public record CreateOrderRequest(string CustomerName, decimal Amount);
public record ProcessPaymentRequest(decimal Amount);

[ApiController]
[Route("api/actor-orders")]
public class ActorOrdersController : ControllerBase
{
    private readonly IActorRef _actor;

    public ActorOrdersController(IRequiredActor<OrderManagerActor> actorRef)
    {
        _actor = actorRef.ActorRef;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CustomerName) || req.Amount <= 0)
        {
            return BadRequest(new OrderError("Invalid customer name or amount"));
        }

        var response = await _actor.Ask<OrderCreated>(
            new CreateOrder(req.CustomerName, req.Amount), 
            TimeSpan.FromSeconds(3));

        return Created($"/api/actor-orders/{response.OrderId}", response);
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay(Guid id, [FromBody] ProcessPaymentRequest req)
    {
        var response = await _actor.Ask<object>(
            new ProcessPayment(id, req.Amount), 
            TimeSpan.FromSeconds(3));

        return response switch
        {
            OrderDetails details => Ok(details),
            OrderError error when error.Message == "Order not found" => NotFound(error),
            OrderError error => BadRequest(error),
            _ => StatusCode(500)
        };
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var response = await _actor.Ask<object>(
            new CancelOrder(id), 
            TimeSpan.FromSeconds(3));

        return response switch
        {
            OrderDetails details => Ok(details),
            OrderError error when error.Message == "Order not found" => NotFound(error),
            OrderError error => BadRequest(error),
            _ => StatusCode(500)
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStatus(Guid id)
    {
        var response = await _actor.Ask<object>(
            new GetOrderStatus(id), 
            TimeSpan.FromSeconds(3));

        return response switch
        {
            OrderDetails details => Ok(details),
            OrderError error when error.Message == "Order not found" => NotFound(error),
            _ => NotFound()
        };
    }
}
