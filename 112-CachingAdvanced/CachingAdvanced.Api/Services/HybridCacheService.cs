using Microsoft.Extensions.Caching.Hybrid;
using CachingAdvanced.Api.Data;
using CachingAdvanced.Api.Dtos;

namespace CachingAdvanced.Api.Services;

public class HybridCacheService
{
    private readonly HybridCache _cache;
    private readonly AppDbContext _db;
    
    public HybridCacheService(HybridCache cache, AppDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    public async Task<ProductDto?> GetProductAsync(int id, CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(
            $"hybrid:product:{id}",
            async innerCt => {
                var product = await _db.Products.FindAsync(new object[] { id }, innerCt);
                return product == null ? null : new ProductDto(product.Id, product.Name, product.Price, product.Stock);
            },
            new HybridCacheEntryOptions {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            },
            cancellationToken: ct);
    }
    
    public async Task InvalidateAsync(int id, CancellationToken ct = default)
        => await _cache.RemoveAsync($"hybrid:product:{id}", ct);
}
