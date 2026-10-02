namespace MultiTenant.Api.Core.Entities;

/// <summary>
/// Thực thể Đơn hàng thuộc sở hữu của một Tenant cụ thể.
/// </summary>
public class Order : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = default!; // Phân vùng dữ liệu
    public string CustomerName { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
