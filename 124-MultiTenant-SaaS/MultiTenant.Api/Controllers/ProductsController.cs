using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenant.Api.Core.Entities;
using MultiTenant.Api.Core.Interfaces;
using MultiTenant.Api.Infrastructure.Context;

namespace MultiTenant.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ProductsController(ApplicationDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm.
    /// CHÚ Ý: Lập trình viên không cần viết .Where(p => p.TenantId == ...).
    /// EF Core Global Query Filter tự động lọc chính xác sản phẩm thuộc về Tenant đang gọi!
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll()
    {
        var products = await _db.Products
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync();

        return Ok(new
        {
            tenant = _tenantContext.CurrentTenant?.Name,
            tenantId = _tenantContext.TenantId,
            tier = _tenantContext.CurrentTenant?.Tier.ToString(),
            databaseMode = string.IsNullOrEmpty(_tenantContext.CurrentTenant?.DedicatedConnectionString) 
                ? "Shared Database (Row-level Isolation)" 
                : "Dedicated Database (Physical Isolation)",
            count = products.Count,
            data = products
        });
    }

    /// <summary>
    /// Lấy chi tiết sản phẩm theo ID.
    /// Nếu sản phẩm thuộc về Tenant khác, EF Core tự động coi như không tồn tại (trả về 404).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(Guid id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound(new { error = $"Không tìm thấy sản phẩm hoặc sản phẩm không thuộc quyền quản lý của Tenant '{_tenantContext.TenantId}'." });
        }
        return Ok(product);
    }

    /// <summary>
    /// Thêm sản phẩm mới.
    /// TenantId được DbContext tự động gán từ ngữ cảnh request, developer không thể gán nhầm.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Sku))
        {
            return BadRequest(new { error = "Tên sản phẩm và SKU là bắt buộc." });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Sku = request.Sku.Trim().ToUpperInvariant(),
            Price = request.Price,
            StockQuantity = request.StockQuantity
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// Xóa sản phẩm.
    /// Nhờ Global Query Filter, Tenant không thể xóa nhầm sản phẩm của Tenant khác.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound(new { error = "Không tìm thấy sản phẩm để xóa." });
        }

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateProductRequest(
    string Name,
    string Sku,
    decimal Price,
    int StockQuantity
);
