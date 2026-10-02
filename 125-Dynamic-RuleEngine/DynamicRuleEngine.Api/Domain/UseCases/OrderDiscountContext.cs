namespace DynamicRuleEngine.Api.Domain.UseCases;

/// <summary>
/// Ngữ cảnh tính chiết khấu giỏ hàng thương mại điện tử.
/// </summary>
public class OrderDiscountContext
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerTier { get; set; } = "Standard"; // Standard, VIP, Diamond
    public decimal TotalOrderAmount { get; set; }
    public int ItemCount { get; set; }
    public bool IsWeekend { get; set; }
}
