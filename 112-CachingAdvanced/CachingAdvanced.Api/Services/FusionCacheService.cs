using ZiggyCreatures.Caching.Fusion;
using CachingAdvanced.Api.Data;
using CachingAdvanced.Api.Dtos;

namespace CachingAdvanced.Api.Services;

public class FusionCacheService
{
    private readonly IFusionCache _cache;
    private readonly AppDbContext _db;
    
    public FusionCacheService(IFusionCache cache, AppDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        return await _cache.GetOrSetAsync(
            $"fusion:product:{id}",
            async ct => {
                var product = await _db.Products.FindAsync(new object[] { id }, ct);
                return product == null ? null : new ProductDto(product.Id, product.Name, product.Price, product.Stock);
            },
            new FusionCacheEntryOptions {
                Duration = TimeSpan.FromMinutes(10),
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(1),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
                FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
                FactoryHardTimeout = TimeSpan.FromMilliseconds(500)
            });
    }
}
