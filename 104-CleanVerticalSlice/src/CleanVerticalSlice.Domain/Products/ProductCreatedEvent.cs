namespace CleanVerticalSlice.Domain.Products;

using CleanVerticalSlice.Domain.Common;

public class ProductCreatedEvent : IDomainEvent
{
    public Product Product { get; }

    public ProductCreatedEvent(Product product)
    {
        Product = product;
    }
}
