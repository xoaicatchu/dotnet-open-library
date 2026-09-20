namespace CleanVerticalSlice.Domain.Orders;

using CleanVerticalSlice.Domain.Common;

public class OrderItem : BaseEntity
{
    public int ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int OrderId { get; private set; }

    private OrderItem() { }

    public OrderItem(int productId, int quantity, decimal unitPrice)
    {
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
