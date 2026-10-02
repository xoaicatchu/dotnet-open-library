using Microsoft.AspNetCore.Http;
using MultiTenant.Api.Core.Interfaces;

namespace MultiTenant.Api.Infrastructure.Strategies;

/// <summary>
/// Trích xuất Tenant từ Subdomain (Ví dụ: "acme.saas.local:5000" => "acme").
/// </summary>
public class SubdomainTenantResolutionStrategy : ITenantResolutionStrategy
{
    public int Priority => 2;

    public Task<string?> ResolveTenantIdentifierAsync(HttpContext context)
    {
        var host = context.Request.Host.Host; // Bỏ port nếu có
        var parts = host.Split('.');

        // Nếu host có ít nhất 2 phần (vd: acme.localhost hoặc acme.saas.local)
        // và không phải địa chỉ IP trực tiếp
        if (parts.Length >= 2 && !System.Net.IPAddress.TryParse(host, out _))
        {
            var subdomain = parts[0].ToLowerInvariant().Trim();
            if (subdomain != "www" && subdomain != "api" && subdomain != "localhost")
            {
                return Task.FromResult<string?>(subdomain);
            }
        }

        return Task.FromResult<string?>(null);
    }
}
