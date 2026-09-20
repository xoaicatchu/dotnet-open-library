using GraphQL.Api.Data;
using GraphQL.Api.Entities;
using GraphQL.Api.Subscriptions;
using HotChocolate;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using System;
using System.Threading.Tasks;

namespace GraphQL.Api.Mutations;

[MutationType]
public class ProductMutation
{
    public async Task<ProductEntity> CreateProduct(
        string name, string description, decimal price, int stock, int categoryId,
        [Service] AppDbContext db,
        [Service] ITopicEventSender eventSender)
    {
        var product = new ProductEntity {
            Name = name, Description = description, Price = price,
            Stock = stock, CategoryId = categoryId, CreatedAt = DateTime.UtcNow
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();
        // Trigger subscription event
        await eventSender.SendAsync(nameof(ProductSubscription.OnProductCreated), product);
        return product;
    }
    
    public async Task<ProductEntity?> UpdateProduct(
        int id, string? name, decimal? price, int? stock,
        [Service] AppDbContext db)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return null;
        if (name != null) product.Name = name;
        if (price.HasValue) product.Price = price.Value;
        if (stock.HasValue) product.Stock = stock.Value;
        await db.SaveChangesAsync();
        return product;
    }
    
    public async Task<bool> DeleteProduct(int id, [Service] AppDbContext db)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return false;
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return true;
    }
}
