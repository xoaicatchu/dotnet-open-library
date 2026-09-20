using AutoMapper;
using EnterpriseClassic.Api.Data;
using FluentValidation;
using Hangfire;
using MassTransit;
using MediatR;

namespace EnterpriseClassic.Api.Features.Orders;

public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<OrderDto>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateOrderRequest> _validator;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public CreateOrderCommandHandler(
        AppDbContext db, 
        IMapper mapper, 
        IValidator<CreateOrderRequest> validator, 
        IPublishEndpoint publishEndpoint, 
        IBackgroundJobClient backgroundJobClient)
    {
        _db = db;
        _mapper = mapper;
        _validator = validator;
        _publishEndpoint = publishEndpoint;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request.Request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var order = new OrderEntity
        {
            CustomerName = request.Request.CustomerName,
            CustomerEmail = request.Request.CustomerEmail,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            TotalAmount = request.Request.Items.Sum(i => i.Quantity * i.UnitPrice),
            Items = request.Request.Items.Select(i => new OrderItemEntity
            {
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        await _publishEndpoint.Publish(new OrderCreatedEvent(order.Id, order.CustomerEmail), cancellationToken);

        _backgroundJobClient.Enqueue(() => Console.WriteLine($"Order {order.Id} created for {order.CustomerEmail}"));

        return _mapper.Map<OrderDto>(order);
    }
}
