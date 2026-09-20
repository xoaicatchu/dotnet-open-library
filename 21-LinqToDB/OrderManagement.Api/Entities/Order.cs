using LinqToDB.Mapping;

namespace OrderManagement.Api.Entities;

[Table("Orders")]
public class Order
{
    [PrimaryKey, Identity]
    public int Id { get; set; }

    [Column(Length = 100), NotNull]
    public string CustomerName { get; set; } = string.Empty;

    [Column(Length = 200), NotNull]
    public string ShippingAddress { get; set; } = string.Empty;

    [Column(Length = 50), NotNull]
    public string Status { get; set; } = "Pending";

    [Column, NotNull]
    public decimal TotalAmount { get; set; }

    [Column, NotNull]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column, Nullable]
    public DateTime? UpdatedAt { get; set; }

    [Association(ThisKey = nameof(Id), OtherKey = nameof(OrderItem.OrderId))]
    public IEnumerable<OrderItem> Items { get; set; } = [];
}
