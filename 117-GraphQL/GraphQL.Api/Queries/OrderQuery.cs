using GraphQL.Api.Data;
using GraphQL.Api.Entities;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GraphQL.Api.Queries;

[QueryType]
public class OrderQuery
{
    [UseProjection]
    public IQueryable<OrderEntity> GetOrders([Service] AppDbContext db)
        => db.Orders.Include(o => o.Product);
}
