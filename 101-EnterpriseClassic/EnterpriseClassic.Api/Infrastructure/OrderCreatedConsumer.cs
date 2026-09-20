using EnterpriseClassic.Api.Features.Orders;
using MassTransit;

namespace EnterpriseClassic.Api.Infrastructure;

public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    public Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        Console.WriteLine($"OrderCreatedConsumer: Sending email to {context.Message.CustomerEmail} for Order {context.Message.OrderId}");
        return Task.CompletedTask;
    }
}
