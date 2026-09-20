using Microsoft.Extensions.Caching.Memory;
using CachingAdvanced.Api.Data;
using CachingAdvanced.Api.Dtos;

namespace CachingAdvanced.Api.Services;

public class MemoryCacheService
{
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _db;
    
    public MemoryCacheService(IMemoryCache cache, AppDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var cacheKey = $"product:{id}";
        if (_cache.TryGetValue(cacheKey, out ProductDto? cached)) return cached;
        
        var product = await _db.Products.FindAsync(id);
        if (product == null) return null;
        
        var dto = new ProductDto(product.Id, product.Name, product.Price, product.Stock);
        _cache.Set(cacheKey, dto, new MemoryCacheEntryOptions {
            SlidingExpiration = TimeSpan.FromMinutes(5),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            Priority = CacheItemPriority.High
        });
        return dto;
    }
    
    public void Invalidate(int id) => _cache.Remove($"product:{id}");
    public void InvalidateAll() => (_cache as MemoryCache)?.Compact(1.0);
}
