using DotNetCore.CAP;

namespace ModernHighPerf.Api.Features.Orders;

public class OrderCreatedEventHandler : ICapSubscribe
{
    [CapSubscribe("order.created")]
    public void Handle(OrderCreatedEvent @event)
    {
        Console.WriteLine($"[CAP Event Received] Order {@event.OrderId} created. Email: {@event.CustomerEmail}");
    }
}
