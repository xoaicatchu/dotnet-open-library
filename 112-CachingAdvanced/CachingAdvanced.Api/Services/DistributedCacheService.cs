using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using CachingAdvanced.Api.Data;
using CachingAdvanced.Api.Dtos;

namespace CachingAdvanced.Api.Services;

public class DistributedCacheService
{
    private readonly IDistributedCache _cache;
    private readonly AppDbContext _db;
    
    public DistributedCacheService(IDistributedCache cache, AppDbContext db)
    {
        _cache = cache;
        _db = db;
    }

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var cacheKey = $"dist:product:{id}";
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null) return JsonSerializer.Deserialize<ProductDto>(cached);
        
        var product = await _db.Products.FindAsync(id);
        if (product == null) return null;
        
        var dto = new ProductDto(product.Id, product.Name, product.Price, product.Stock);
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
            new DistributedCacheEntryOptions {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        return dto;
    }
    
    public async Task InvalidateAsync(int id) => await _cache.RemoveAsync($"dist:product:{id}");
}
