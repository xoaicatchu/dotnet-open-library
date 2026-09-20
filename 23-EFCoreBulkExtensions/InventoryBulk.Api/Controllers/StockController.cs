using System.Diagnostics;
using InventoryBulk.Api.Data;
using InventoryBulk.Api.Entities;
using InventoryBulk.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryBulk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly InventoryDbContext _db;

    public StockController(InventoryDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Gets all stock items with optional category filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StockItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StockItemDto>>> GetAll([FromQuery] string? category)
    {
        var query = _db.StockItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(s => s.Category == category);
        }

        var items = await query
            .OrderBy(s => s.Id)
            .Select(s => new StockItemDto(s.Id, s.Sku, s.Name, s.Category, s.Price, s.Quantity, s.UpdatedAt))
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Gets stock item by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StockItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockItemDto>> GetById(int id)
    {
        var item = await _db.StockItems.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (item == null)
        {
            return NotFound(new { message = $"Stock item with ID {id} not found." });
        }

        return Ok(new StockItemDto(item.Id, item.Sku, item.Name, item.Category, item.Price, item.Quantity, item.UpdatedAt));
    }

    /// <summary>
    /// Batch Insert: Inserts multiple items using EF Core AddRange + SaveChangesAsync.
    /// Demonstrates high-performance batch insertion without third-party extensions.
    /// </summary>
    [HttpPost("bulk-insert")]
    [ProducesResponseType(typeof(BulkResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkResultDto>> BulkInsert([FromBody] BulkInsertRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "At least one item is required." });
        }

        var now = DateTime.UtcNow;
        var entities = request.Items.Select(dto => new StockItem
        {
            Sku = dto.Sku.Trim(),
            Name = dto.Name.Trim(),
            Category = dto.Category.Trim(),
            Price = dto.Price,
            Quantity = dto.Quantity,
            UpdatedAt = now
        }).ToList();

        var sw = Stopwatch.StartNew();
        // EF Core batches INSERT statements automatically when using AddRange
        _db.StockItems.AddRange(entities);
        await _db.SaveChangesAsync();
        sw.Stop();

        return Ok(new BulkResultDto(
            "BulkInsert",
            entities.Count,
            sw.ElapsedMilliseconds,
            $"Inserted {entities.Count} items in {sw.ElapsedMilliseconds}ms."
        ));
    }

    /// <summary>
    /// Bulk Update: Uses EF Core 7+ ExecuteUpdateAsync to update rows directly in the database
    /// without loading entities into memory.
    /// </summary>
    [HttpPost("bulk-update")]
    [ProducesResponseType(typeof(BulkResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkResultDto>> BulkUpdate([FromBody] BulkUpdatePriceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest(new { message = "Category is required." });
        }

        if (request.PriceMultiplier <= 0)
        {
            return BadRequest(new { message = "Price multiplier must be > 0." });
        }

        var sw = Stopwatch.StartNew();
        // EF Core 7+ ExecuteUpdateAsync: compiles to a single UPDATE...WHERE SQL statement
        // No entity is loaded into memory — zero allocation overhead
        var affected = await _db.StockItems
            .Where(s => s.Category == request.Category)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Price, s => s.Price * request.PriceMultiplier)
                .SetProperty(s => s.UpdatedAt, _ => DateTime.UtcNow));
        sw.Stop();

        return Ok(new BulkResultDto(
            "BulkUpdate",
            affected,
            sw.ElapsedMilliseconds,
            $"Updated {affected} items in '{request.Category}' in {sw.ElapsedMilliseconds}ms."
        ));
    }

    /// <summary>
    /// Bulk Delete: Uses EF Core 7+ ExecuteDeleteAsync to delete rows directly in the database.
    /// </summary>
    [HttpPost("bulk-delete")]
    [ProducesResponseType(typeof(BulkResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkResultDto>> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Category))
        {
            return BadRequest(new { message = "Category is required." });
        }

        var sw = Stopwatch.StartNew();
        // EF Core 7+ ExecuteDeleteAsync: compiles to a single DELETE...WHERE SQL statement
        var affected = await _db.StockItems
            .Where(s => s.Category == request.Category)
            .ExecuteDeleteAsync();
        sw.Stop();

        return Ok(new BulkResultDto(
            "BulkDelete",
            affected,
            sw.ElapsedMilliseconds,
            $"Deleted {affected} items from '{request.Category}' in {sw.ElapsedMilliseconds}ms."
        ));
    }

    /// <summary>
    /// Bulk Upsert: Inserts new items or updates existing ones matched by SKU.
    /// Uses EF Core change tracking + manual merge logic.
    /// </summary>
    [HttpPost("bulk-upsert")]
    [ProducesResponseType(typeof(BulkResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkResultDto>> BulkUpsert([FromBody] BulkUpsertRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "At least one item is required." });
        }

        var now = DateTime.UtcNow;
        var incomingSkus = request.Items.Select(i => i.Sku.Trim()).ToList();

        // Load existing items by SKU for merge
        var existingItems = await _db.StockItems
            .Where(s => incomingSkus.Contains(s.Sku))
            .ToDictionaryAsync(s => s.Sku);

        var newItems = new List<StockItem>();

        foreach (var dto in request.Items)
        {
            var sku = dto.Sku.Trim();
            if (existingItems.TryGetValue(sku, out var existing))
            {
                // Update existing
                existing.Name = dto.Name.Trim();
                existing.Category = dto.Category.Trim();
                existing.Price = dto.Price;
                existing.Quantity = dto.Quantity;
                existing.UpdatedAt = now;
            }
            else
            {
                // Insert new
                newItems.Add(new StockItem
                {
                    Sku = sku,
                    Name = dto.Name.Trim(),
                    Category = dto.Category.Trim(),
                    Price = dto.Price,
                    Quantity = dto.Quantity,
                    UpdatedAt = now
                });
            }
        }

        var sw = Stopwatch.StartNew();
        if (newItems.Count > 0)
        {
            _db.StockItems.AddRange(newItems);
        }
        await _db.SaveChangesAsync();
        sw.Stop();

        var totalAffected = existingItems.Count + newItems.Count;
        return Ok(new BulkResultDto(
            "BulkInsertOrUpdate",
            totalAffected,
            sw.ElapsedMilliseconds,
            $"Upserted {totalAffected} items ({existingItems.Count} updated, {newItems.Count} inserted) in {sw.ElapsedMilliseconds}ms."
        ));
    }

    /// <summary>
    /// Gets aggregate count per category.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _db.StockItems
            .AsNoTracking()
            .GroupBy(s => s.Category)
            .Select(g => new
            {
                Category = g.Key,
                ItemCount = g.Count(),
                TotalQuantity = g.Sum(x => x.Quantity),
                AvgPrice = Math.Round(g.Average(x => x.Price), 2)
            })
            .ToListAsync();

        return Ok(summary);
    }
}
