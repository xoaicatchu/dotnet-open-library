using MassTransit;
using OrderProcessor.Api.Contracts;
using OrderProcessor.Api.Data;

namespace OrderProcessor.Api.Consumers;

public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    private readonly OrderStore _store;

    public OrderSubmittedConsumer(OrderStore store)
    {
        _store = store;
    }
    
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var order = _store.GetById(context.Message.OrderId);
        if (order != null)
        {
            _store.UpdateStatus(order.Id, "Processing");
            // Simulate processing delay
            await Task.Delay(100);
            
            // Re-fetch to check if cancelled during processing
            order = _store.GetById(context.Message.OrderId);
            if (order != null && order.Status != "Cancelled")
            {
                _store.UpdateStatus(order.Id, "Completed");
                await context.Publish(new OrderProcessed(order.Id, DateTime.UtcNow));
            }
        }
    }
}
