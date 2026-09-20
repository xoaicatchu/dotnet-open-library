using Grpc.Core;
using gRPCStreaming.Shared;

namespace gRPCStreaming.Server.Services;

public class ProductStreamService : ProductStream.ProductStreamBase
{
    private static readonly List<(int Id, string Name, double Price, int Stock)> _products = [
        (1, "iPhone 15", 999.99, 50), (2, "MacBook Pro", 2499.99, 20),
        (3, "iPad Air", 599.99, 100), (4, "AirPods Pro", 249.99, 200)
    ];
    
    // Unary
    public override Task<ProductReply> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        var p = _products.FirstOrDefault(p => p.Id == request.Id);
        return Task.FromResult(p == default
            ? throw new RpcException(new Status(StatusCode.NotFound, $"Product {request.Id} not found"))
            : new ProductReply { Id = p.Id, Name = p.Name, Price = p.Price, Stock = p.Stock });
    }
    
    // Server Streaming: stream simulated price updates
    public override async Task StreamPrices(
        StreamPricesRequest request,
        IServerStreamWriter<PriceUpdate> responseStream,
        ServerCallContext context)
    {
        var rng = new Random();
        var count = Math.Min(request.Count, 10); // max 10 updates
        for (int i = 0; i < count && !context.CancellationToken.IsCancellationRequested; i++)
        {
            var basePrice = _products.FirstOrDefault(p => p.Id == request.ProductId).Price;
            var fluctuation = (rng.NextDouble() - 0.5) * 10;
            await responseStream.WriteAsync(new PriceUpdate {
                ProductId = request.ProductId,
                Price = Math.Round(basePrice + fluctuation, 2),
                Timestamp = DateTime.UtcNow.ToString("O")
            });
            await Task.Delay(100, context.CancellationToken); // simulate real-time delay
        }
    }
    
    // Server Streaming: list all products
    public override async Task ListProducts(
        ListProductsRequest request,
        IServerStreamWriter<ProductReply> responseStream,
        ServerCallContext context)
    {
        var pageSize = request.PageSize > 0 ? request.PageSize : 10;
        foreach (var p in _products.Take(pageSize))
        {
            if (context.CancellationToken.IsCancellationRequested) break;
            await responseStream.WriteAsync(
                new ProductReply { Id = p.Id, Name = p.Name, Price = p.Price, Stock = p.Stock });
            await Task.Delay(50, context.CancellationToken);
        }
    }
}
