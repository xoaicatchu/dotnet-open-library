using Mapster;
using Microsoft.EntityFrameworkCore;
using ModernHighPerf.Api.Data;

namespace ModernHighPerf.Api.Features.Products;

public class GetProductsQueryHandler
{
    private readonly AppDbContext _db;
    public GetProductsQueryHandler(AppDbContext db) => _db = db;
    
    public async Task<List<ProductSummaryDto>> Handle(GetProductsQuery query)
    {
        var products = await _db.Products.AsNoTracking().ToListAsync();
        return products.Select(p => p.Adapt<ProductSummaryDto>()).ToList();
    }
}
