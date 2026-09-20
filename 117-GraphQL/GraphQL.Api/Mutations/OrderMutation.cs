using GraphQL.Api.Data;
using GraphQL.Api.Entities;
using HotChocolate;
using HotChocolate.Types;
using System;
using System.Threading.Tasks;

namespace GraphQL.Api.Mutations;

[MutationType]
public class OrderMutation
{
    public async Task<OrderEntity?> CreateOrder(
        string customerName, int productId, int quantity,
        [Service] AppDbContext db)
    {
        var product = await db.Products.FindAsync(productId);
        if (product == null || product.Stock < quantity) return null;
        var order = new OrderEntity {
            CustomerName = customerName,
            ProductId = productId,
            Quantity = quantity,
            TotalPrice = product.Price * quantity,
            CreatedAt = DateTime.UtcNow
        };
        product.Stock -= quantity;
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }
}
