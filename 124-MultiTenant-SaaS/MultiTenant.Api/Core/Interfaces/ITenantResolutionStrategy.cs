using Microsoft.AspNetCore.Http;

namespace MultiTenant.Api.Core.Interfaces;

/// <summary>
/// Chiến lược trích xuất định danh Tenant từ HTTP Request (Header, Subdomain, JWT, v.v.).
/// </summary>
public interface ITenantResolutionStrategy
{
    int Priority { get; } // Độ ưu tiên thực hiện
    Task<string?> ResolveTenantIdentifierAsync(HttpContext context);
}
