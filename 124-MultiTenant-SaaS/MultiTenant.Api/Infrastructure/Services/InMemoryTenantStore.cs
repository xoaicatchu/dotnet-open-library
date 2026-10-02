using System.Collections.Concurrent;
using MultiTenant.Api.Core.Entities;
using MultiTenant.Api.Core.Interfaces;

namespace MultiTenant.Api.Infrastructure.Services;

/// <summary>
/// Kho lưu trữ Tenant Catalog trong bộ nhớ (hỗ trợ tra cứu nhanh & thread-safe).
/// </summary>
public class InMemoryTenantStore : ITenantStore
{
    private readonly ConcurrentDictionary<string, Tenant> _tenants = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryTenantStore()
    {
        // Khởi tạo các Tenant mẫu cho Demo & Testing
        SeedDefaultTenants();
    }

    private void SeedDefaultTenants()
    {
        var tenant1 = new Tenant
        {
            Id = "tenant-standard-1",
            Name = "Acme Corporation",
            Subdomain = "acme",
            Tier = TenantTier.Standard,
            DedicatedConnectionString = null, // Dùng chung Shared DB
            IsActive = true
        };

        var tenant2 = new Tenant
        {
            Id = "tenant-standard-2",
            Name = "Beta Logistics",
            Subdomain = "beta",
            Tier = TenantTier.Standard,
            DedicatedConnectionString = null, // Dùng chung Shared DB
            IsActive = true
        };

        var tenantVip = new Tenant
        {
            Id = "tenant-enterprise-vip",
            Name = "VIP Global Holdings",
            Subdomain = "vip",
            Tier = TenantTier.Enterprise,
            DedicatedConnectionString = "Data Source=tenant_vip.db", // Dedicated SQLite DB
            IsActive = true
        };

        var tenantSuspended = new Tenant
        {
            Id = "tenant-suspended",
            Name = "Suspended Corp",
            Subdomain = "suspended",
            Tier = TenantTier.Standard,
            IsActive = false // Bị khóa
        };

        _tenants[tenant1.Id] = tenant1;
        _tenants[tenant2.Id] = tenant2;
        _tenants[tenantVip.Id] = tenantVip;
        _tenants[tenantSuspended.Id] = tenantSuspended;
    }

    public Task<Tenant?> GetTenantByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        // Cho phép tìm kiếm bằng Id hoặc Subdomain
        var tenant = _tenants.Values.FirstOrDefault(t => 
            string.Equals(t.Id, identifier, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(t.Subdomain, identifier, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(tenant);
    }

    public Task<IReadOnlyList<Tenant>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Tenant> list = _tenants.Values.ToList();
        return Task.FromResult(list);
    }

    public Task<Tenant> AddTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _tenants[tenant.Id] = tenant;
        return Task.FromResult(tenant);
    }
}
