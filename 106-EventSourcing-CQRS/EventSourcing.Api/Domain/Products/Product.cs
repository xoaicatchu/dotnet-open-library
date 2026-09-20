using System;
using EventSourcing.Api.Domain.Common;
using EventSourcing.Api.Domain.Products.Events;
namespace EventSourcing.Api.Domain.Products;

public class Product : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public bool IsActive { get; private set; }
    
    public Product() { } // for reconstruction
    
    public static Product Create(string name, decimal price, int stock)
    {
        var product = new Product();
        product.RaiseEvent(new ProductCreated(Guid.NewGuid(), name, price, stock, DateTime.UtcNow));
        return product;
    }
    
    public void Update(string name, decimal price, int stock)
    {
        if (!IsActive) throw new InvalidOperationException("Cannot update inactive product");
        RaiseEvent(new ProductUpdated(Id, name, price, stock, DateTime.UtcNow));
    }
    
    public void Deactivate()
    {
        if (!IsActive) throw new InvalidOperationException("Already inactive");
        RaiseEvent(new ProductDeactivated(Id, DateTime.UtcNow));
    }
    
    protected override void Apply(IEvent @event)
    {
        switch (@event)
        {
            case ProductCreated e:
                Id = e.AggregateId;
                Name = e.Name;
                Price = e.Price;
                Stock = e.Stock;
                IsActive = true;
                break;
            case ProductUpdated e:
                Name = e.Name;
                Price = e.Price;
                Stock = e.Stock;
                break;
            case ProductDeactivated:
                IsActive = false;
                break;
        }
    }
}
