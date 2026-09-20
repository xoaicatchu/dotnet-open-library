namespace OrderService.Entities;

public class OrderItemEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
