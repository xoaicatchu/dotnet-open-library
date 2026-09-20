namespace CleanVerticalSlice.Domain.Products;

using System;
using CleanVerticalSlice.Domain.Common;

public class Product : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    public int Stock { get; private set; }
    public int CategoryId { get; private set; }
    
    private Product() { } // EF Core
    
    public static Product Create(string name, string description, decimal price, int stock, int categoryId)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Price = new Money(price),
            Stock = stock,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };
        product.AddDomainEvent(new ProductCreatedEvent(product));
        return product;
    }
    
    public void Update(string name, string description, decimal price, int stock)
    {
        Name = name;
        Description = description;
        Price = new Money(price);
        Stock = stock;
        UpdatedAt = DateTime.UtcNow;
    }
}
