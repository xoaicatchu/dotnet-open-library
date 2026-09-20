using OrderMapping.Api.Entities;

namespace OrderMapping.Api.Data;

public class OrderStore
{
    private readonly List<Order> _orders = new();
    private int _nextId = 1;

    public OrderStore()
    {
        Seed();
    }

    private void Seed()
    {
        _orders.Add(new Order
        {
            Id = _nextId++,
            OrderNumber = "ORD-2026-0001",
            Customer = new Customer
            {
                Id = 101,
                FirstName = "Alice",
                LastName = "Smith",
                Email = "alice.smith@example.com"
            },
            ShippingAddress = new Address
            {
                Street = "123 Main St",
                City = "Seattle",
                State = "WA",
                ZipCode = "98101",
                Country = "USA"
            },
            Items = new List<OrderItem>
            {
                new() { Id = 1, ProductName = "Wireless Mouse", UnitPrice = 25.50m, Quantity = 2 },
                new() { Id = 2, ProductName = "USB-C Hub", UnitPrice = 45.00m, Quantity = 1 }
            },
            Status = "Processing",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
    }

    public List<Order> GetAll() => _orders.ToList();

    public Order? GetById(int id) => _orders.FirstOrDefault(o => o.Id == id);

    public Order Add(Order order)
    {
        order.Id = _nextId++;
        order.OrderNumber = $"ORD-2026-{order.Id:D4}";
        order.Customer.Id = 100 + order.Id;
        int itemId = 1;
        foreach (var item in order.Items)
        {
            item.Id = itemId++;
        }
        order.CreatedAt = DateTime.UtcNow;
        order.Status = "Created";
        _orders.Add(order);
        return order;
    }
}
