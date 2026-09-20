using LinqToDB.Mapping;

namespace OrderManagement.Api.Entities;

[Table("OrderItems")]
public class OrderItem
{
    [PrimaryKey, Identity]
    public int Id { get; set; }

    [Column, NotNull]
    public int OrderId { get; set; }

    [Column(Length = 100), NotNull]
    public string ProductName { get; set; } = string.Empty;

    [Column, NotNull]
    public int Quantity { get; set; }

    [Column, NotNull]
    public decimal UnitPrice { get; set; }

    [Association(ThisKey = nameof(OrderId), OtherKey = nameof(Order.Id))]
    public Order? Order { get; set; }
}
