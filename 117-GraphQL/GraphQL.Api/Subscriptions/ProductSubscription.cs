using GraphQL.Api.Entities;
using HotChocolate;
using HotChocolate.Types;

namespace GraphQL.Api.Subscriptions;

[SubscriptionType]
public class ProductSubscription
{
    [Subscribe]
    [Topic]
    public ProductEntity OnProductCreated([EventMessage] ProductEntity product) => product;
}
