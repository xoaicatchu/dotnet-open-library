using ProductCatalogCache.Api.Models;

namespace ProductCatalogCache.Api.Services;

public interface IProductCatalogService
{
    Task<ProductItem> GetProductFromDatabaseAsync(int id, CancellationToken cancellationToken = default);
    void SetFailureSimulation(bool enabled);
    bool IsFailureSimulated { get; }
    int TotalDbQueries { get; }
    void ResetStats();
}

public class ProductCatalogService : IProductCatalogService
{
    private int _totalDbQueries;
    private bool _isFailureSimulated;

    public int TotalDbQueries => _totalDbQueries;
    public bool IsFailureSimulated => _isFailureSimulated;

    public void SetFailureSimulation(bool enabled)
    {
        _isFailureSimulated = enabled;
    }

    public void ResetStats()
    {
        Interlocked.Exchange(ref _totalDbQueries, 0);
        _isFailureSimulated = false;
    }

    public async Task<ProductItem> GetProductFromDatabaseAsync(int id, CancellationToken cancellationToken = default)
    {
        if (_isFailureSimulated)
        {
            throw new InvalidOperationException("Simulated Database Outage / Network Timeout!");
        }

        // Simulate database query latency
        await Task.Delay(50, cancellationToken);
        Interlocked.Increment(ref _totalDbQueries);

        return new ProductItem(
            Id: id,
            Name: $"Product SKU-{id:D4}",
            Price: 99.99m + (id * 5.0m),
            Stock: 100 - id,
            FetchedAt: DateTime.UtcNow
        );
    }
}
