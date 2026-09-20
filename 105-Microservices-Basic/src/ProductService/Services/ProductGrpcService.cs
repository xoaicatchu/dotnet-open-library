using Grpc.Core;
using Shared.Contracts;
using ProductService.Data;

namespace ProductService.Services;

public class ProductGrpcService : ProductGrpc.ProductGrpcBase
{
    private readonly ProductDbContext _db;
    public ProductGrpcService(ProductDbContext db) => _db = db;

    public override async Task<GetProductReply> GetProduct(GetProductRequest request, ServerCallContext ctx)
    {
        var p = await _db.Products.FindAsync(request.Id);
        return p == null
            ? new GetProductReply { Found = false }
            : new GetProductReply { Id = p.Id, Name = p.Name, Price = (double)p.Price, Stock = p.Stock, Found = true };
    }

    public override async Task<CheckStockReply> CheckStock(CheckStockRequest request, ServerCallContext ctx)
    {
        var p = await _db.Products.FindAsync(request.ProductId);
        if (p == null) return new CheckStockReply { Available = false, Message = "Product not found" };
        return p.Stock >= request.Quantity
            ? new CheckStockReply { Available = true, Message = "In stock" }
            : new CheckStockReply { Available = false, Message = $"Only {p.Stock} available" };
    }
}
