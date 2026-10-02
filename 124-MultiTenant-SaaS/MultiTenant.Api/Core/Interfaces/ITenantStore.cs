using MultiTenant.Api.Core.Entities;

namespace MultiTenant.Api.Core.Interfaces;

/// <summary>
/// Kho lưu trữ và tra cứu thông tin Tenant từ Master Catalog.
/// </summary>
public interface ITenantStore
{
    Task<Tenant?> GetTenantByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default);
    Task<Tenant> AddTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
