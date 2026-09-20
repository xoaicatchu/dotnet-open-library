namespace CleanVerticalSlice.Application.Orders.GetOrders;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using CleanVerticalSlice.Application.Common.Interfaces;

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, List<OrderSummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetOrdersHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderSummaryDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                CustomerEmail = o.CustomerEmail,
                Status = o.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
