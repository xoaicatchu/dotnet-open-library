namespace CleanVerticalSlice.Application.Orders.CreateOrder;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CleanVerticalSlice.Application.Common.Interfaces;
using CleanVerticalSlice.Application.Orders.GetOrders;
using CleanVerticalSlice.Domain.Orders;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public CreateOrderHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderSummaryDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var items = request.Request.Items.Select(i => new OrderItem(i.ProductId, i.Quantity, i.UnitPrice)).ToList();
        var entity = Order.Create(request.Request.CustomerEmail, items);

        _context.Orders.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return new OrderSummaryDto
        {
            Id = entity.Id,
            CustomerEmail = entity.CustomerEmail,
            Status = entity.Status.ToString()
        };
    }
}
