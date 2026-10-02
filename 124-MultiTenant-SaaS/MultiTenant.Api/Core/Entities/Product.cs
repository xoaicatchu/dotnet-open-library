namespace MultiTenant.Api.Core.Entities;

/// <summary>
/// Thực thể Sản phẩm thuộc sở hữu của một Tenant cụ thể.
/// </summary>
public class Product : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TenantId { get; set; } = default!; // Phân vùng dữ liệu
    public string Name { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
