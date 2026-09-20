using CachingAdvanced.Api.Dtos;

namespace CachingAdvanced.Api.Services;

public interface IProductCacheService
{
    Task<ProductDto?> GetProductAsync(int id, CancellationToken ct = default);
    Task InvalidateAsync(int id, CancellationToken ct = default);
}
