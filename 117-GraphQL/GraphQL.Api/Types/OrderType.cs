using GraphQL.Api.Entities;
using HotChocolate.Types;
using GraphQL.Api.DataLoaders;

namespace GraphQL.Api.Types;

public class OrderType : ObjectType<OrderEntity>
{
    protected override void Configure(IObjectTypeDescriptor<OrderEntity> descriptor)
    {
        descriptor.Field(o => o.Id).Type<NonNullType<IdType>>();
        descriptor.Field(o => o.CustomerName).Type<NonNullType<StringType>>();
        descriptor.Field(o => o.ProductId).Type<NonNullType<IntType>>();
        descriptor.Field(o => o.Quantity).Type<NonNullType<IntType>>();
        descriptor.Field(o => o.TotalPrice).Type<NonNullType<DecimalType>>();
        descriptor.Field(o => o.CreatedAt).Type<NonNullType<DateTimeType>>();
        
        descriptor.Field(o => o.Product)
            .Type<NonNullType<ProductType>>()
            .Resolve(async (ctx, ct) => 
            {
                var order = ctx.Parent<OrderEntity>();
                return await ctx.DataLoader<ProductByIdDataLoader>().LoadAsync(order.ProductId, ct);
            });
    }
}
