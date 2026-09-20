using MassTransit;
using OrderProcessor.Api.Contracts;
using OrderProcessor.Api.Data;

namespace OrderProcessor.Api.Consumers;

public class OrderProcessedConsumer : IConsumer<OrderProcessed>
{
    private readonly OrderStore _store;

    public OrderProcessedConsumer(OrderStore store)
    {
        _store = store;
    }

    public Task Consume(ConsumeContext<OrderProcessed> context)
    {
        _store.UpdateStatus(context.Message.OrderId, "Completed");
        return Task.CompletedTask;
    }
}
