namespace CleanVerticalSlice.Domain.Orders;

using CleanVerticalSlice.Domain.Common;

public class OrderCreatedEvent : IDomainEvent
{
    public Order Order { get; }

    public OrderCreatedEvent(Order order)
    {
        Order = order;
    }
}
