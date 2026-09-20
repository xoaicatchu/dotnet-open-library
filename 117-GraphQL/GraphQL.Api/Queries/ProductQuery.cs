using GraphQL.Api.Data;
using GraphQL.Api.Entities;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using System.Linq;

namespace GraphQL.Api.Queries;

[QueryType]
public class ProductQuery
{
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ProductEntity> GetProducts([Service] AppDbContext db)
        => db.Products;
    
    [UsePaging(IncludeTotalCount = true, DefaultPageSize = 10)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ProductEntity> GetProductsPaged([Service] AppDbContext db)
        => db.Products;
    
    public async Task<ProductEntity?> GetProductById(int id, [Service] AppDbContext db)
        => await db.Products.FindAsync(id);
}
