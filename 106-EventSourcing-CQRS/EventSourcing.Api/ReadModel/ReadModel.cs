using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Api.Domain.Products.Events;
namespace EventSourcing.Api.ReadModel;

public class ProductReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
}

public interface IProductReadRepository
{
    void Apply(ProductCreated e);
    void Apply(ProductUpdated e);
    void Apply(ProductDeactivated e);
    Task<List<ProductReadModel>> GetAllAsync();
    Task<ProductReadModel?> GetByIdAsync(Guid id);
}

public class InMemoryProductReadRepository : IProductReadRepository
{
    private readonly ConcurrentDictionary<Guid, ProductReadModel> _store = new();
    
    public void Apply(ProductCreated e) => _store[e.AggregateId] = new ProductReadModel
    {
        Id = e.AggregateId, Name = e.Name, Price = e.Price, Stock = e.Stock, IsActive = true, Version = 1
    };
    
    public void Apply(ProductUpdated e)
    {
        if (_store.TryGetValue(e.AggregateId, out var m))
        { m.Name = e.Name; m.Price = e.Price; m.Stock = e.Stock; m.Version++; }
    }
    
    public void Apply(ProductDeactivated e)
    {
        if (_store.TryGetValue(e.AggregateId, out var m))
        { m.IsActive = false; m.Version++; }
    }
    
    public Task<List<ProductReadModel>> GetAllAsync() => Task.FromResult(_store.Values.ToList());
    public Task<ProductReadModel?> GetByIdAsync(Guid id) =>
        Task.FromResult(_store.TryGetValue(id, out var m) ? m : null);
}
