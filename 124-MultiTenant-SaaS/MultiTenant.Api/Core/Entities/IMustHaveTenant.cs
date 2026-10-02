namespace MultiTenant.Api.Core.Entities;

/// <summary>
/// Đánh dấu thực thể bắt buộc phải thuộc về một Tenant cụ thể.
/// EF Core sẽ dựa vào interface này để tự động cấu hình Global Query Filter
/// và tự động gán TenantId trước khi lưu vào Database.
/// </summary>
public interface IMustHaveTenant
{
    string TenantId { get; set; }
}
