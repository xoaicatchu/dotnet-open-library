using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Observability.Api.Data;
using Observability.Api.Entities;
using Observability.Api.Metrics;
using Observability.Api.Tracing;

namespace Observability.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AppMetrics _metrics;
    private readonly ILogger<ProductsController> _logger;
    
    public ProductsController(AppDbContext db, AppMetrics metrics, ILogger<ProductsController> logger)
    {
        _db = db;
        _metrics = metrics;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<ProductEntity>>> GetAll()
    {
        using var activity = ActivitySources.Api.StartActivity("GetProducts");
        var sw = Stopwatch.StartNew();
        
        _metrics.RecordRequest("/api/products", "GET");
        _logger.LogInformation("Fetching all products");
        
        var products = await _db.Products.AsNoTracking().ToListAsync();
        
        activity?.SetTag("products.count", products.Count);
        _metrics.RecordDuration(sw.ElapsedMilliseconds, "/api/products");
        
        return Ok(products);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductEntity>> GetById(int id)
    {
        using var activity = ActivitySources.Api.StartActivity("GetProductById");
        activity?.SetTag("product.id", id);
        var sw = Stopwatch.StartNew();
        
        _metrics.RecordRequest($"/api/products/{id}", "GET");
        _logger.LogInformation("Fetching product {Id}", id);
        
        var product = await _db.Products.FindAsync(id);
        
        _metrics.RecordDuration(sw.ElapsedMilliseconds, $"/api/products/{id}");
        
        if (product == null)
        {
            _logger.LogWarning("Product {Id} not found", id);
            return NotFound();
        }
        
        return Ok(product);
    }
    
    [HttpPost]
    public async Task<ActionResult<ProductEntity>> Create([FromBody] CreateProductRequest req)
    {
        using var activity = ActivitySources.Api.StartActivity("CreateProduct");
        var sw = Stopwatch.StartNew();
        _metrics.RecordRequest("/api/products", "POST");
        _logger.LogInformation("Creating product: {Name}", req.Name);
        
        var product = new ProductEntity { Name = req.Name, Price = req.Price, Stock = req.Stock, CreatedAt = DateTime.UtcNow };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        
        activity?.SetTag("product.id", product.Id);
        _metrics.RecordProductCreated();
        _metrics.IncrementActiveProducts();
        _metrics.RecordDuration(sw.ElapsedMilliseconds, "/api/products");
        _logger.LogInformation("Product created with Id={ProductId}", product.Id);
        
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var activity = ActivitySources.Api.StartActivity("DeleteProduct");
        activity?.SetTag("product.id", id);
        var sw = Stopwatch.StartNew();
        _metrics.RecordRequest($"/api/products/{id}", "DELETE");
        _logger.LogInformation("Deleting product {Id}", id);
        
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            _metrics.RecordDuration(sw.ElapsedMilliseconds, $"/api/products/{id}");
            _logger.LogWarning("Product {Id} not found for deletion", id);
            return NotFound();
        }
        
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        
        _metrics.DecrementActiveProducts();
        _metrics.RecordDuration(sw.ElapsedMilliseconds, $"/api/products/{id}");
        _logger.LogInformation("Product {Id} deleted", id);
        
        return NoContent();
    }
}
