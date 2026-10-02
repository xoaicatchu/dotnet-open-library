using Microsoft.AspNetCore.Http;
using MultiTenant.Api.Core.Interfaces;

namespace MultiTenant.Api.Infrastructure.Strategies;

/// <summary>
/// Trích xuất Tenant từ HTTP Header: "X-Tenant-Id".
/// Phù hợp cho API Client, Mobile App, Postman hoặc Swagger.
/// </summary>
public class HeaderTenantResolutionStrategy : ITenantResolutionStrategy
{
    public const string HeaderName = "X-Tenant-Id";

    public int Priority => 1; // Độ ưu tiên cao nhất

    public Task<string?> ResolveTenantIdentifierAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var tenantId = values.FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(tenantId))
            {
                return Task.FromResult<string?>(tenantId);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
