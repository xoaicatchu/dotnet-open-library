using DotNetCore.CAP;
using Hangfire;
using Mapster;
using ModernHighPerf.Api.Data;

namespace ModernHighPerf.Api.Features.Orders;

public class CreateOrderCommandHandler
{
    private readonly AppDbContext _db;
    private readonly ICapPublisher _capPublisher;
    private readonly IBackgroundJobClient _backgroundJobs;

    public CreateOrderCommandHandler(AppDbContext db, ICapPublisher capPublisher, IBackgroundJobClient backgroundJobs)
    {
        _db = db;
        _capPublisher = capPublisher;
        _backgroundJobs = backgroundJobs;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand command)
    {
        var order = command.Request.Adapt<OrderEntity>();
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        order.Status = "Created";

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Publish event with CAP
        await _capPublisher.PublishAsync("order.created", new OrderCreatedEvent(order.Id, order.CustomerEmail));

        // Enqueue background job with Hangfire
        _backgroundJobs.Enqueue(() => Console.WriteLine($"Background Job: Order {order.Id} created for {order.CustomerName}"));

        return order.Adapt<OrderDto>();
    }
}
