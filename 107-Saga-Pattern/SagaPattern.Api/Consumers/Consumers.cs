using System.Threading.Tasks;
using MassTransit;
using SagaPattern.Api.Messages;

namespace SagaPattern.Api.Consumers;

public class PaymentConsumer : IConsumer<ProcessPaymentCommand>
{
    public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
    {
        var msg = context.Message;
        if (msg.Amount > 500)
            await context.Publish(new PaymentFailed(msg.OrderId, "Amount exceeds limit"));
        else
            await context.Publish(new PaymentProcessed(msg.OrderId));
    }
}

public class InventoryConsumer : IConsumer<ReserveInventoryCommand>
{
    public async Task Consume(ConsumeContext<ReserveInventoryCommand> context)
    {
        var msg = context.Message;
        if (msg.Quantity > 10)
            await context.Publish(new InventoryFailed(msg.OrderId, "Insufficient stock"));
        else
            await context.Publish(new InventoryReserved(msg.OrderId));
    }
}

public class ShippingConsumer : IConsumer<ShipOrderCommand>
{
    public async Task Consume(ConsumeContext<ShipOrderCommand> context)
    {
        await context.Publish(new OrderShipped(context.Message.OrderId));
    }
}
