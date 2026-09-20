using System.Diagnostics;
using System.Diagnostics.Metrics;
using Confluent.Kafka;
using DotNetCore.CAP;
using Hangfire;
using MassTransit;
using MediatR;
using MQTTnet;
using MQTTnet.Client;
using NLog;
using OpenTelemetry;
using Quartz;
using Rebus.Bus;
using Serilog;
using AllInOne.Api.Data;
using AllInOne.Api.Models;

namespace AllInOne.Api.Services;

// MediatR Query Handler (Library 04)
public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly OrderDataService _dataService;

    public GetOrderByIdQueryHandler(OrderDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return await _dataService.GetOrderByIdEfAsync(request.Id);
    }
}

public class OrderMessagingService
{
    private static readonly ActivitySource ActivitySource = new("AllInOne.Orders", "1.0.0");
    private static readonly Meter Meter = new("AllInOne.Orders", "1.0.0");
    private static readonly Counter<long> OrderCounter = Meter.CreateCounter<long>("orders_placed_total");
    private static readonly Logger NLogger = LogManager.GetCurrentClassLogger();

    private readonly IPublishEndpoint _massTransitBus;
    private readonly ICapPublisher _capPublisher;
    private readonly IBackgroundJobClient _hangfireClient;

    public OrderMessagingService(
        IPublishEndpoint massTransitBus,
        ICapPublisher capPublisher,
        IBackgroundJobClient hangfireClient)
    {
        _massTransitBus = massTransitBus;
        _capPublisher = capPublisher;
        _hangfireClient = hangfireClient;
    }

    public async Task NotifyOrderLifecycleAsync(OrderDto order)
    {
        // 1. OpenTelemetry (Library 40) - Distributed Tracing & Metrics
        using var activity = ActivitySource.StartActivity("ProcessOrderLifecycle");
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("order.number", order.OrderNumber);
        activity?.SetTag("order.amount", order.TotalAmount);
        OrderCounter.Add(1, new KeyValuePair<string, object?>("status", order.Status));

        // 2. Serilog (Library 38) & NLog (Library 39) - Structured & Audit Logging
        Serilog.Log.Information("Processing order {OrderNumber} for {Customer} with amount ${Amount}",
            order.OrderNumber, order.CustomerName, order.TotalAmount);
        NLogger.Info($"[AuditLog] Order {order.OrderNumber} status: {order.Status}");

        // 3. MassTransit (Library 06) - Publish In-Memory Message
        await _massTransitBus.Publish(new OrderSubmittedMessage(order.Id, order.OrderNumber, order.TotalAmount));

        // 4. CAP (Library 07) - Transactional Outbox Event
        await _capPublisher.PublishAsync("order.placed", new { order.Id, order.OrderNumber });

        // 5. Hangfire (Library 16) - Fire-and-forget background job
        _hangfireClient.Enqueue(() => Console.WriteLine($"[Hangfire] Follow-up email scheduled for {order.CustomerEmail}"));
    }
}

public record OrderSubmittedMessage(int OrderId, string OrderNumber, decimal TotalAmount);
