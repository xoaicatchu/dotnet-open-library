using GraphQL.Api.Entities;
using HotChocolate.Types;

namespace GraphQL.Api.Types;

public class ProductType : ObjectType<ProductEntity>
{
    protected override void Configure(IObjectTypeDescriptor<ProductEntity> descriptor)
    {
        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.Name).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Description).Type<NonNullType<StringType>>();
        descriptor.Field(p => p.Price).Type<NonNullType<DecimalType>>();
        descriptor.Field(p => p.Stock).Type<NonNullType<IntType>>();
        descriptor.Field(p => p.CategoryId).Type<NonNullType<IntType>>();
        descriptor.Field(p => p.CreatedAt).Type<NonNullType<DateTimeType>>();
        
        descriptor.Field(p => p.Orders)
            .Type<ListType<NonNullType<OrderType>>>()
            .ResolveWith<ProductResolvers>(r => r.GetOrdersAsync(default!, default!));
    }
    
    private class ProductResolvers
    {
        public async Task<IEnumerable<OrderEntity>> GetOrdersAsync(
            [Parent] ProductEntity product,
            [Service] Data.AppDbContext db)
        {
            return db.Orders.Where(o => o.ProductId == product.Id);
        }
    }
}
