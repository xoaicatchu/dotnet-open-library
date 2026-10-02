namespace MultiTenant.Api.Core.Entities;

public enum TenantTier
{
    Standard = 1,   // Dùng chung Database (Shared DB)
    Enterprise = 2  // Dùng Database riêng biệt (Dedicated DB)
}

/// <summary>
/// Thực thể đại diện cho Tenant (Khách hàng / Doanh nghiệp thuê dịch vụ SaaS).
/// Được lưu trữ trong Master Catalog Database.
/// </summary>
public class Tenant
{
    public string Id { get; set; } = default!; // Mã định danh duy nhất (vd: "tenant-a", "tenant-b")
    public string Name { get; set; } = default!; // Tên công ty/tổ chức
    public string Subdomain { get; set; } = default!; // Subdomain (vd: "acme" trong acme.saas.local)
    public TenantTier Tier { get; set; } = TenantTier.Standard;
    public string? DedicatedConnectionString { get; set; } // Chuỗi kết nối DB riêng (nếu là Enterprise)
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
