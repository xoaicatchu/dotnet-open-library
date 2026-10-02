using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Api.Auth;
using StockFlow.Api.Common;
using StockFlow.Api.Data;
using StockFlow.Api.Inventory;

namespace StockFlow.Api.Products;

[ApiController, Route("api/products"), Authorize]
public sealed class ProductsController(StockFlowDbContext db, InventoryTransactionRunner transactions) : ControllerBase
{
    private const long MaxSafeInteger = 9_007_199_254_740_991;

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] bool? active = null, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest(Problem("InvalidPage", "Trang phải >= 1 và pageSize nằm trong 1..100."));
        var query = db.Products.AsNoTracking();
        if (active is not null) query = query.Where(product => product.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(product => product.Sku.Contains(term) || product.Name.Contains(term) || (product.Barcode != null && product.Barcode.Contains(term)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await (from product in query.OrderBy(product => product.Sku).ThenBy(product => product.Id).Skip((page - 1) * pageSize).Take(pageSize)
                           join balance in db.StockBalances.AsNoTracking() on product.Id equals balance.ProductId into balances
                           from balance in balances.DefaultIfEmpty()
                           select new ProductResponse(product.Id, product.Sku, product.Barcode, product.Name, product.Unit,
                               product.PurchasePriceVnd, product.SalePriceVnd, product.MinimumStock, product.IsActive,
                               balance == null ? 0L : balance.Quantity, balance == null ? 1L : balance.Version))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResponse<ProductResponse>(items, page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var product = await (from item in db.Products.AsNoTracking()
                             where item.Id == id
                             join balance in db.StockBalances.AsNoTracking() on item.Id equals balance.ProductId into balances
                             from balance in balances.DefaultIfEmpty()
                             select new ProductResponse(item.Id, item.Sku, item.Barcode, item.Name, item.Unit,
                                 item.PurchasePriceVnd, item.SalePriceVnd, item.MinimumStock, item.IsActive,
                                 balance == null ? 0L : balance.Quantity, balance == null ? 1L : balance.Version))
            .SingleOrDefaultAsync(cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost, Authorize(Roles = RolePolicies.Admin)]
    public async Task<ActionResult<ProductResponse>> Create(ProductRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(Problem("InvalidProduct", validation));

        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = NormalizeCode(request.Sku),
            Barcode = NormalizeBarcode(request.Barcode),
            Name = request.Name.Trim(),
            Unit = request.Unit.Trim(),
            PurchasePriceVnd = request.PurchasePriceVnd,
            SalePriceVnd = request.SalePriceVnd,
            MinimumStock = request.MinimumStock,
            IsActive = request.IsActive
        };
        try
        {
            var response = await transactions.ExecuteAsync(async token =>
            {
                db.Products.Add(product);
                db.StockBalances.Add(new StockBalance { ProductId = product.Id, Quantity = 0, Version = 1 });
                await db.SaveChangesAsync(token);
                return ToResponse(product, 0, 1);
            }, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = product.Id }, response);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(Problem("DuplicateProduct", "SKU hoặc mã vạch đã được sử dụng.", StatusCodes.Status409Conflict));
        }
        catch (DbUpdateException exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(Problem("DuplicateProduct", "SKU hoặc mã vạch đã được sử dụng.", StatusCodes.Status409Conflict));
        }
    }

    [HttpPut("{id:guid}"), Authorize(Roles = RolePolicies.Admin)]
    public async Task<ActionResult<ProductResponse>> Update(Guid id, ProductRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(Problem("InvalidProduct", validation));
        var product = await db.Products.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null) return NotFound();
        var unit = request.Unit.Trim();
        if (!string.Equals(product.Unit, unit, StringComparison.Ordinal) && await db.StockMovements.AnyAsync(movement => movement.ProductId == id, cancellationToken))
            return Conflict(Problem("UnitInUse", "Không thể đổi đơn vị sau khi mặt hàng đã có biến động kho.", StatusCodes.Status409Conflict));

        product.Sku = NormalizeCode(request.Sku);
        product.Barcode = NormalizeBarcode(request.Barcode);
        product.Name = request.Name.Trim();
        product.Unit = unit;
        product.PurchasePriceVnd = request.PurchasePriceVnd;
        product.SalePriceVnd = request.SalePriceVnd;
        product.MinimumStock = request.MinimumStock;
        product.IsActive = request.IsActive;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(Problem("DuplicateProduct", "SKU hoặc mã vạch đã được sử dụng.", StatusCodes.Status409Conflict));
        }
        var balance = await db.StockBalances.AsNoTracking().SingleOrDefaultAsync(item => item.ProductId == id, cancellationToken);
        return Ok(ToResponse(product, balance?.Quantity ?? 0, balance?.Version ?? 1));
    }

    private static string? Validate(ProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) || request.Sku.Trim().Length > 64) return "SKU không được để trống và tối đa 64 ký tự.";
        if (request.Barcode?.Trim().Length > 128) return "Mã vạch tối đa 128 ký tự.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200) return "Tên mặt hàng không được để trống và tối đa 200 ký tự.";
        if (string.IsNullOrWhiteSpace(request.Unit) || request.Unit.Trim().Length > 32) return "Đơn vị không được để trống và tối đa 32 ký tự.";
        if (request.PurchasePriceVnd is < 0 or > MaxSafeInteger || request.SalePriceVnd is < 0 or > MaxSafeInteger || request.MinimumStock is < 0 or > MaxSafeInteger)
            return "Giá và tồn tối thiểu phải là số nguyên không âm trong giới hạn an toàn.";
        return null;
    }

    private static string NormalizeCode(string value) => value.Trim().ToUpperInvariant();
    private static string? NormalizeBarcode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ProductResponse ToResponse(Product product, long quantity, long version) => new(product.Id, product.Sku, product.Barcode,
        product.Name, product.Unit, product.PurchasePriceVnd, product.SalePriceVnd, product.MinimumStock, product.IsActive, quantity, version);
    private static ProblemDetails Problem(string code, string title, int status = StatusCodes.Status400BadRequest) => new() { Status = status, Title = title, Extensions = { ["code"] = code } };
    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException postgres) return postgres;
        return null;
    }
}
