using Mapster;
using Microsoft.EntityFrameworkCore;
using ModernHighPerf.Api.Data;

namespace ModernHighPerf.Api.Features.Orders;

public class GetOrdersQueryHandler
{
    private readonly AppDbContext _db;
    public GetOrdersQueryHandler(AppDbContext db) => _db = db;
    
    public async Task<List<OrderDto>> Handle(GetOrdersQuery query)
    {
        var orders = await _db.Orders.Include(o => o.Items).AsNoTracking().ToListAsync();
        return orders.Select(o => o.Adapt<OrderDto>()).ToList();
    }
}
