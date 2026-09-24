using MassTransit;
using AiAgentMessaging.Api.Ai;
using AiAgentMessaging.Api.Contracts;
using AiAgentMessaging.Api.Data;

namespace AiAgentMessaging.Api.Consumers;

public class OrderSubmittedConsumer(OrderStore store, IAiOrderEnricher aiEnricher, ILogger<OrderSubmittedConsumer> logger)
    : IConsumer<OrderSubmitted>
{
    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var msg = context.Message;
        var order = store.GetById(msg.OrderId);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found", msg.OrderId);
            return;
        }

        logger.LogInformation("Processing order {OrderId} for {Customer}", msg.OrderId, msg.CustomerName);

        // AI enrichment - pluggable, fails gracefully
        var enrichment = await aiEnricher.EnrichAsync(
            new OrderEnrichmentRequest(msg.OrderId, msg.CustomerName, msg.Product, msg.Quantity, msg.TotalPrice),
            context.CancellationToken);

        // Update order with AI results and mark as processed
        store.Update(msg.OrderId, o =>
        {
            o.Status = "Processed";
            o.AiCategory = enrichment.Category;
            o.AiSummary = enrichment.Summary;
            o.AiProvider = enrichment.Provider;
        });

        logger.LogInformation(
            "Order {OrderId} processed. AI={IsFromAi}, Category={Category}, Provider={Provider}",
            msg.OrderId, enrichment.IsFromAi, enrichment.Category, enrichment.Provider);
    }
}
