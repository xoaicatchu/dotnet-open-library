using Microsoft.AspNetCore.Mvc;
using WarehouseManagement.Api.Entities;
using WarehouseManagement.Api.Models;
using WarehouseManagement.Api.Repositories;

namespace WarehouseManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly IWarehouseRepository _repository;

    public ItemsController(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all warehouse items with optional location or keyword filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ItemResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ItemResponseDto>>> GetAll(
        [FromQuery] string? location,
        [FromQuery] string? search)
    {
        var items = await _repository.GetAllAsync(location, search);
        var response = items.Select(i => new ItemResponseDto(
            i.Id,
            i.Sku,
            i.Name,
            i.Location,
            i.Quantity,
            i.UnitCost,
            i.Quantity * i.UnitCost,
            i.LastRestockedAt
        ));
        return Ok(response);
    }

    /// <summary>
    /// Gets item by ID.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemResponseDto>> GetById(long id)
    {
        var item = await _repository.GetByIdAsync(id);
        if (item == null)
        {
            return NotFound(new { message = $"Warehouse item with ID {id} not found." });
        }

        var response = new ItemResponseDto(
            item.Id,
            item.Sku,
            item.Name,
            item.Location,
            item.Quantity,
            item.UnitCost,
            item.Quantity * item.UnitCost,
            item.LastRestockedAt
        );
        return Ok(response);
    }

    /// <summary>
    /// Creates a new warehouse item.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ItemResponseDto>> Create([FromBody] CreateItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Sku))
        {
            return BadRequest(new { message = "SKU is required." });
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Item name is required." });
        }

        if (dto.Quantity < 0 || dto.UnitCost < 0)
        {
            return BadRequest(new { message = "Quantity and UnitCost must be non-negative." });
        }

        var existing = await _repository.GetBySkuAsync(dto.Sku.Trim());
        if (existing != null)
        {
            return BadRequest(new { message = $"Item with SKU '{dto.Sku}' already exists." });
        }

        var item = new WarehouseItem
        {
            Sku = dto.Sku.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Location = dto.Location.Trim(),
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost,
            LastRestockedAt = DateTime.UtcNow
        };

        var id = await _repository.CreateAsync(item);
        var created = await _repository.GetByIdAsync(id);

        var response = new ItemResponseDto(
            created!.Id,
            created.Sku,
            created.Name,
            created.Location,
            created.Quantity,
            created.UnitCost,
            created.Quantity * created.UnitCost,
            created.LastRestockedAt
        );

        return CreatedAtAction(nameof(GetById), new { id }, response);
    }

    /// <summary>
    /// Updates an existing warehouse item.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemResponseDto>> Update(long id, [FromBody] UpdateItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Item name is required." });
        }

        if (dto.Quantity < 0 || dto.UnitCost < 0)
        {
            return BadRequest(new { message = "Quantity and UnitCost must be non-negative." });
        }

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = $"Warehouse item with ID {id} not found." });
        }

        existing.Name = dto.Name.Trim();
        existing.Location = dto.Location.Trim();
        existing.Quantity = dto.Quantity;
        existing.UnitCost = dto.UnitCost;

        await _repository.UpdateAsync(existing);
        var updated = await _repository.GetByIdAsync(id);

        var response = new ItemResponseDto(
            updated!.Id,
            updated.Sku,
            updated.Name,
            updated.Location,
            updated.Quantity,
            updated.UnitCost,
            updated.Quantity * updated.UnitCost,
            updated.LastRestockedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Upserts (inserts or updates) warehouse item by SKU.
    /// Demonstrates RepoDb Merge operation.
    /// </summary>
    [HttpPost("upsert")]
    [ProducesResponseType(typeof(ItemResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ItemResponseDto>> Upsert([FromBody] UpsertItemDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Sku) || string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "SKU and Name are required." });
        }

        var item = new WarehouseItem
        {
            Sku = dto.Sku.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Location = dto.Location.Trim(),
            Quantity = dto.Quantity,
            UnitCost = dto.UnitCost,
            LastRestockedAt = DateTime.UtcNow
        };

        var id = await _repository.UpsertAsync(item);
        var upserted = await _repository.GetByIdAsync(id);

        var response = new ItemResponseDto(
            upserted!.Id,
            upserted.Sku,
            upserted.Name,
            upserted.Location,
            upserted.Quantity,
            upserted.UnitCost,
            upserted.Quantity * upserted.UnitCost,
            upserted.LastRestockedAt
        );

        return Ok(response);
    }

    /// <summary>
    /// Batch restocks multiple items atomically using RepoDb UpdateAll within transaction.
    /// </summary>
    [HttpPost("batch-restock")]
    [ProducesResponseType(typeof(BatchRestockResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchRestockResult>> BatchRestock([FromBody] BatchRestockRequest request)
    {
        if (request.Skus == null || request.Skus.Count == 0)
        {
            return BadRequest(new { message = "At least one SKU is required." });
        }

        if (request.AddedQuantity <= 0)
        {
            return BadRequest(new { message = "Added quantity must be greater than 0." });
        }

        var updated = await _repository.BatchRestockAsync(request.Skus, request.AddedQuantity);
        return Ok(new BatchRestockResult(updated, $"Successfully restocked {updated} items with +{request.AddedQuantity} units each."));
    }

    /// <summary>
    /// Deletes warehouse item by ID.
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = $"Warehouse item with ID {id} not found." });
        }

        await _repository.DeleteAsync(id);
        return NoContent();
    }
}
