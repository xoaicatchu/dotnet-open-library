namespace CleanVerticalSlice.Application.Orders.GetOrders;

public class OrderSummaryDto
{
    public int Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
