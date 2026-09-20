namespace CleanVerticalSlice.Domain.Orders;

using System;
using System.Collections.Generic;
using CleanVerticalSlice.Domain.Common;

public class Order : BaseEntity
{
    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    
    private Order() { }
    
    public static Order Create(string customerEmail, List<OrderItem> items)
    {
        var order = new Order
        {
            CustomerEmail = customerEmail,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        order._items.AddRange(items);
        order.AddDomainEvent(new OrderCreatedEvent(order));
        return order;
    }
}
