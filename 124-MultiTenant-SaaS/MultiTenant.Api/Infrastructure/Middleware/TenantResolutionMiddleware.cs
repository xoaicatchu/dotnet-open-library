using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MultiTenant.Api.Core.Interfaces;

namespace MultiTenant.Api.Infrastructure.Middleware;

/// <summary>
/// Middleware phát hiện và nạp thông tin Tenant vào Scoped Context cho mỗi request.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IEnumerable<ITenantResolutionStrategy> _strategies;

    public TenantResolutionMiddleware(
        RequestDelegate next, 
        IEnumerable<ITenantResolutionStrategy> strategies)
    {
        _next = next;
        _strategies = strategies.OrderBy(s => s.Priority);
    }

    public async Task InvokeAsync(
        HttpContext context, 
        ITenantStore tenantStore, 
        ITenantContext tenantContext)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Bỏ qua các endpoint không yêu cầu ngữ cảnh Tenant cụ thể
        if (IsExemptPath(path))
        {
            await _next(context);
            return;
        }

        // Thực thi các chiến lược trích xuất Tenant theo thứ tự ưu tiên
        string? tenantIdentifier = null;
        foreach (var strategy in _strategies)
        {
            tenantIdentifier = await strategy.ResolveTenantIdentifierAsync(context);
            if (!string.IsNullOrEmpty(tenantIdentifier))
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(tenantIdentifier))
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "Thiếu định danh Tenant. Vui lòng cung cấp Header 'X-Tenant-Id' hoặc truy cập qua Subdomain hợp lệ."
            }));
            return;
        }

        // Tra cứu thông tin Tenant từ Catalog Store
        var tenant = await tenantStore.GetTenantByIdentifierAsync(tenantIdentifier);
        if (tenant == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = $"Không tìm thấy Tenant với định danh '{tenantIdentifier}'."
            }));
            return;
        }

        if (!tenant.IsActive)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = $"Tenant '{tenant.Name}' ({tenant.Id}) hiện đang bị tạm khóa/đình chỉ dịch vụ."
            }));
            return;
        }

        // Nạp Tenant hợp lệ vào Context cho request hiện tại
        tenantContext.SetTenant(tenant);

        // Chèn TenantId vào Response Header để client dễ quan sát/debug
        context.Response.Headers["X-Resolved-Tenant"] = tenant.Id;

        await _next(context);
    }

    private static bool IsExemptPath(string path)
    {
        return path == "/" ||
               path.StartsWith("/swagger") ||
               path.StartsWith("/api/tenants") ||
               path.StartsWith("/health");
    }
}
