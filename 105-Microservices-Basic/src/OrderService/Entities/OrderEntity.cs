namespace OrderService.Entities;

public class OrderEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemEntity> Items { get; set; } = new();
}
