using Mapster;
using ModernHighPerf.Api.Data;
using ZiggyCreatures.Caching.Fusion;

namespace ModernHighPerf.Api.Features.Products;

public class UpdateProductCommandHandler
{
    private readonly AppDbContext _db;
    private readonly IFusionCache _cache;

    public UpdateProductCommandHandler(AppDbContext db, IFusionCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ProductDetailDto?> Handle(UpdateProductCommand command)
    {
        var product = await _db.Products.FindAsync(command.Id);
        if (product == null) return null;

        command.Request.Adapt(product);
        await _db.SaveChangesAsync();

        await _cache.RemoveAsync($"product_{command.Id}");

        return product.Adapt<ProductDetailDto>();
    }
}
