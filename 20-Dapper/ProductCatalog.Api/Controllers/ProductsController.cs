using Microsoft.AspNetCore.Mvc;
using ProductCatalog.Api.Entities;
using ProductCatalog.Api.Models;
using ProductCatalog.Api.Repositories;

namespace ProductCatalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;

    public ProductsController(IProductRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all products with optional filtering by name/description search or category.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAll([FromQuery] string? search, [FromQuery] int? categoryId)
    {
        var products = await _repository.GetAllAsync(search, categoryId);
        var response = products.Select(p => new ProductResponseDto(
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.Stock,
            p.CategoryId,
            p.Category?.Name
        ));
        return Ok(response);
    }

    /// <summary>
    /// Gets product by ID with joined Category details.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> GetById(int id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        var response = new ProductResponseDto(
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.Stock,
            product.CategoryId,
            product.Category?.Name
        );
        return Ok(response);
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponseDto>> Create([FromBody] CreateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Product name is required." });
        }

        if (dto.Price < 0)
        {
            return BadRequest(new { message = "Price cannot be negative." });
        }

        if (dto.Stock < 0)
        {
            return BadRequest(new { message = "Stock cannot be negative." });
        }

        var categoryExists = await _repository.CategoryExistsAsync(dto.CategoryId);
        if (!categoryExists)
        {
            return BadRequest(new { message = $"Category with ID {dto.CategoryId} does not exist." });
        }

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Price = dto.Price,
            Stock = dto.Stock,
            CategoryId = dto.CategoryId
        };

        var newId = await _repository.CreateAsync(product);
        var created = await _repository.GetByIdAsync(newId);

        var response = new ProductResponseDto(
            created!.Id,
            created.Name,
            created.Description,
            created.Price,
            created.Stock,
            created.CategoryId,
            created.Category?.Name
        );

        return CreatedAtAction(nameof(GetById), new { id = newId }, response);
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponseDto>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return BadRequest(new { message = "Product name is required." });
        }

        if (dto.Price < 0)
        {
            return BadRequest(new { message = "Price cannot be negative." });
        }

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        var categoryExists = await _repository.CategoryExistsAsync(dto.CategoryId);
        if (!categoryExists)
        {
            return BadRequest(new { message = $"Category with ID {dto.CategoryId} does not exist." });
        }

        existing.Name = dto.Name.Trim();
        existing.Description = dto.Description.Trim();
        existing.Price = dto.Price;
        existing.Stock = dto.Stock;
        existing.CategoryId = dto.CategoryId;

        await _repository.UpdateAsync(existing);
        var updated = await _repository.GetByIdAsync(id);

        var response = new ProductResponseDto(
            updated!.Id,
            updated.Name,
            updated.Description,
            updated.Price,
            updated.Stock,
            updated.CategoryId,
            updated.Category?.Name
        );

        return Ok(response);
    }

    /// <summary>
    /// Deletes a product by ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        await _repository.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Applies a batch discount to selected products atomically in a transaction.
    /// </summary>
    [HttpPost("batch-discount")]
    [ProducesResponseType(typeof(BatchDiscountResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BatchDiscountResult>> BatchDiscount([FromBody] BatchDiscountRequest request)
    {
        if (request.DiscountPercentage <= 0 || request.DiscountPercentage > 100)
        {
            return BadRequest(new { message = "Discount percentage must be between 0.01 and 100." });
        }

        if (request.ProductIds == null || request.ProductIds.Count == 0)
        {
            return BadRequest(new { message = "At least one product ID must be provided." });
        }

        var affected = await _repository.ApplyBatchDiscountAsync(request.DiscountPercentage, request.ProductIds);
        return Ok(new BatchDiscountResult(affected, $"Successfully applied {request.DiscountPercentage}% discount to {affected} product(s)."));
    }
}
