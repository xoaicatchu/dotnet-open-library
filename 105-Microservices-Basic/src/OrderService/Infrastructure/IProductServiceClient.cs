using Shared.Contracts;

namespace OrderService.Infrastructure;

public interface IProductServiceClient
{
    Task<(bool Found, string Name, decimal Price)> GetProductAsync(int productId);
    Task<(bool Available, string Message)> CheckStockAsync(int productId, int quantity);
}

public class ProductServiceGrpcClient : IProductServiceClient
{
    private readonly ProductGrpc.ProductGrpcClient _client;
    public ProductServiceGrpcClient(ProductGrpc.ProductGrpcClient client) => _client = client;
    
    public async Task<(bool Found, string Name, decimal Price)> GetProductAsync(int productId)
    {
        var res = await _client.GetProductAsync(new GetProductRequest { Id = productId });
        return (res.Found, res.Name, (decimal)res.Price);
    }
    
    public async Task<(bool Available, string Message)> CheckStockAsync(int productId, int quantity)
    {
        var res = await _client.CheckStockAsync(new CheckStockRequest { ProductId = productId, Quantity = quantity });
        return (res.Available, res.Message);
    }
}

public class MockProductServiceClient : IProductServiceClient
{
    public Task<(bool Found, string Name, decimal Price)> GetProductAsync(int productId)
        => Task.FromResult((true, "Test Product", 99.99m));
    public Task<(bool Available, string Message)> CheckStockAsync(int productId, int quantity)
    {
        if (quantity > 10) return Task.FromResult((false, "Not enough stock"));
        return Task.FromResult((true, "In stock"));
    }
}
