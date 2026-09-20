namespace OrderMapping.Api.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Customer Customer { get; set; } = new();
    public Address ShippingAddress { get; set; } = new();
    public Address? BillingAddress { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
