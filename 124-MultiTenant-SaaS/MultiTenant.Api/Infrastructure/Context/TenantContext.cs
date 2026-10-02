using MultiTenant.Api.Core.Entities;
using MultiTenant.Api.Core.Interfaces;

namespace MultiTenant.Api.Infrastructure.Context;

/// <summary>
/// Triển khai ITenantContext dạng Scoped service cho mỗi HTTP Request.
/// </summary>
public class TenantContext : ITenantContext
{
    public Tenant? CurrentTenant { get; private set; }

    public string? TenantId => CurrentTenant?.Id;
    public bool HasTenant => CurrentTenant != null;

    public void SetTenant(Tenant tenant)
    {
        CurrentTenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
    }
}
