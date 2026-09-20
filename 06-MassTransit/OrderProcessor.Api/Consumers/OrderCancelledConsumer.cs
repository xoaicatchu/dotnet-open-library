using MassTransit;
using OrderProcessor.Api.Contracts;
using OrderProcessor.Api.Data;

namespace OrderProcessor.Api.Consumers;

public class OrderCancelledConsumer : IConsumer<OrderCancelled>
{
    private readonly OrderStore _store;

    public OrderCancelledConsumer(OrderStore store)
    {
        _store = store;
    }

    public Task Consume(ConsumeContext<OrderCancelled> context)
    {
        _store.UpdateStatus(context.Message.OrderId, "Cancelled");
        return Task.CompletedTask;
    }
}
