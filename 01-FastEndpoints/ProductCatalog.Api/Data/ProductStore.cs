using System.Collections.Concurrent;
using ProductCatalog.Api.Models;

namespace ProductCatalog.Api.Data;

public class ProductStore
{
    private readonly ConcurrentDictionary<int, Product> _products = new();
    private int _nextId = 0;

    public ProductStore()
    {
        // Seed data
        var p1 = new Product { Id = GetNextId(), Name = "Mechanical Keyboard", Price = 1500000m, Stock = 15 };
        var p2 = new Product { Id = GetNextId(), Name = "Wireless Mouse", Price = 350000m, Stock = 50 };
        
        _products.TryAdd(p1.Id, p1);
        _products.TryAdd(p2.Id, p2);
    }

    private int GetNextId() => Interlocked.Increment(ref _nextId);

    public IEnumerable<Product> GetAll() => _products.Values;

    public Product? GetById(int id) => _products.TryGetValue(id, out var product) ? product : null;

    public Product Create(Product product)
    {
        product.Id = GetNextId();
        _products.TryAdd(product.Id, product);
        return product;
    }

    public bool Update(Product product)
    {
        if (!_products.ContainsKey(product.Id))
            return false;
            
        _products[product.Id] = product;
        return true;
    }

    public bool Delete(int id)
    {
        return _products.TryRemove(id, out _);
    }
}
