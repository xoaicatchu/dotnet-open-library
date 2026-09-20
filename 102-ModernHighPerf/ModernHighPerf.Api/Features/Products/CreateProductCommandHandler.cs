using Mapster;
using ModernHighPerf.Api.Data;
using ZiggyCreatures.Caching.Fusion;

namespace ModernHighPerf.Api.Features.Products;

public class CreateProductCommandHandler
{
    private readonly AppDbContext _db;

    public CreateProductCommandHandler(AppDbContext db) => _db = db;

    public async Task<ProductDetailDto> Handle(CreateProductCommand command)
    {
        var product = command.Request.Adapt<ProductEntity>();
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        
        return product.Adapt<ProductDetailDto>();
    }
}
