using Microsoft.AspNetCore.Mvc;
using MultiTenant.Api.Core.Entities;
using MultiTenant.Api.Core.Interfaces;

namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController : ControllerBase
{
    private readonly ITenantStore _tenantStore;

    public TenantsController(ITenantStore tenantStore)
    {
        _tenantStore = tenantStore;
    }

    /// <summary>
    /// Lấy danh sách toàn bộ các Tenant trong Master Catalog.
    /// Endpoint này không yêu cầu Header X-Tenant-Id (được miễn trừ).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Tenant>>> GetAll()
    {
        var tenants = await _tenantStore.GetAllTenantsAsync();
        return Ok(tenants);
    }

    /// <summary>
    /// Tra cứu thông tin một Tenant cụ thể qua Id hoặc Subdomain.
    /// </summary>
    [HttpGet("{identifier}")]
    public async Task<ActionResult<Tenant>> GetByIdentifier(string identifier)
    {
        var tenant = await _tenantStore.GetTenantByIdentifierAsync(identifier);
        if (tenant == null)
        {
            return NotFound(new { error = $"Không tìm thấy Tenant '{identifier}'." });
        }
        return Ok(tenant);
    }

    /// <summary>
    /// Đăng ký thêm Tenant mới vào hệ thống SaaS (Onboarding).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Tenant>> Create([FromBody] CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Id và Tên Tenant không được để trống." });
        }

        var existing = await _tenantStore.GetTenantByIdentifierAsync(request.Id);
        if (existing != null)
        {
            return Conflict(new { error = $"Tenant với Id '{request.Id}' đã tồn tại." });
        }

        var newTenant = new Tenant
        {
            Id = request.Id.ToLowerInvariant().Trim(),
            Name = request.Name.Trim(),
            Subdomain = request.Subdomain.ToLowerInvariant().Trim(),
            Tier = request.Tier,
            DedicatedConnectionString = request.Tier == TenantTier.Enterprise 
                ? (request.DedicatedConnectionString ?? $"Data Source=tenant_{request.Id}.db")
                : null,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _tenantStore.AddTenantAsync(newTenant);
        return CreatedAtAction(nameof(GetByIdentifier), new { identifier = newTenant.Id }, newTenant);
    }
}

public record CreateTenantRequest(
    string Id,
    string Name,
    string Subdomain,
    TenantTier Tier = TenantTier.Standard,
    string? DedicatedConnectionString = null
);
