using MultiTenant.Api.Core.Entities;

namespace MultiTenant.Api.Core.Interfaces;

/// <summary>
/// Quản lý thông tin Tenant trong phạm vi vòng đời một HTTP request (Scoped).
/// </summary>
public interface ITenantContext
{
    Tenant? CurrentTenant { get; }
    string? TenantId { get; }
    bool HasTenant => CurrentTenant != null;
    void SetTenant(Tenant tenant);
}
