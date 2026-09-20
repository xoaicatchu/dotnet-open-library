using Mapster;
using ZiggyCreatures.Caching.Fusion;
using ModernHighPerf.Api.Data;

namespace ModernHighPerf.Api.Features.Products;

public class GetProductByIdQueryHandler
{
    private readonly AppDbContext _db;
    private readonly IFusionCache _cache;

    public GetProductByIdQueryHandler(AppDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ProductDetailDto?> Handle(GetProductByIdQuery query)
    {
        return await _cache.GetOrSetAsync($"product_{query.Id}", async _ =>
        {
            var product = await _db.Products.FindAsync(query.Id);
            return product?.Adapt<ProductDetailDto>();
        }, new FusionCacheEntryOptions { Duration = TimeSpan.FromMinutes(5) });
    }
}
