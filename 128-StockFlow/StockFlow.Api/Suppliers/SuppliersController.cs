using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Api.Auth;
using StockFlow.Api.Common;
using StockFlow.Api.Data;

namespace StockFlow.Api.Suppliers;

[ApiController, Route("api/suppliers"), Authorize]
public sealed class SuppliersController(StockFlowDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<SupplierResponse>>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest(Problem("InvalidPage", "Trang phải >= 1 và pageSize nằm trong 1..100."));
        var query = db.Suppliers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(supplier => supplier.Code.Contains(term) || supplier.Name.Contains(term) || (supplier.Phone != null && supplier.Phone.Contains(term)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(supplier => supplier.Code).ThenBy(supplier => supplier.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(supplier => new SupplierResponse(supplier.Id, supplier.Code, supplier.Name, supplier.Phone)).ToListAsync(cancellationToken);
        return Ok(new PagedResponse<SupplierResponse>(items, page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var supplier = await db.Suppliers.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new SupplierResponse(item.Id, item.Code, item.Name, item.Phone)).SingleOrDefaultAsync(cancellationToken);
        return supplier is null ? NotFound() : Ok(supplier);
    }

    [HttpPost, Authorize(Roles = RolePolicies.Admin)]
    public async Task<ActionResult<SupplierResponse>> Create(SupplierRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(Problem("InvalidSupplier", validation));
        var supplier = new Supplier { Id = Guid.NewGuid(), Code = request.Code.Trim().ToUpperInvariant(), Name = request.Name.Trim(), Phone = NormalizePhone(request.Phone) };
        db.Suppliers.Add(supplier);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Mã nhà cung cấp đã được sử dụng.", Extensions = { ["code"] = "DuplicateSupplier" } });
        }
        return CreatedAtAction(nameof(Get), new { id = supplier.Id }, ToResponse(supplier));
    }

    [HttpPut("{id:guid}"), Authorize(Roles = RolePolicies.Admin)]
    public async Task<ActionResult<SupplierResponse>> Update(Guid id, SupplierRequest request, CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(Problem("InvalidSupplier", validation));
        var supplier = await db.Suppliers.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (supplier is null) return NotFound();
        supplier.Code = request.Code.Trim().ToUpperInvariant();
        supplier.Name = request.Name.Trim();
        supplier.Phone = NormalizePhone(request.Phone);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return Conflict(new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = "Mã nhà cung cấp đã được sử dụng.", Extensions = { ["code"] = "DuplicateSupplier" } });
        }
        return Ok(ToResponse(supplier));
    }

    private static string? Validate(SupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 64) return "Mã nhà cung cấp không được để trống và tối đa 64 ký tự.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200) return "Tên nhà cung cấp không được để trống và tối đa 200 ký tự.";
        if (request.Phone?.Trim().Length > 32) return "Số điện thoại tối đa 32 ký tự.";
        return null;
    }

    private static string? NormalizePhone(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static SupplierResponse ToResponse(Supplier supplier) => new(supplier.Id, supplier.Code, supplier.Name, supplier.Phone);
    private static ProblemDetails Problem(string code, string title) => new() { Status = StatusCodes.Status400BadRequest, Title = title, Extensions = { ["code"] = code } };
    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException postgres) return postgres;
        return null;
    }
}
