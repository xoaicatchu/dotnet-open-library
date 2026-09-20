using Microsoft.EntityFrameworkCore;
using TestingMastery.Api.Data;
using TestingMastery.Api.Entities;

namespace TestingMastery.Api.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        return await _db.Products.AsNoTracking()
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock))
            .ToListAsync();
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return null;
        return new ProductDto(p.Id, p.Name, p.Price, p.Stock);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest req)
    {
        var entity = new ProductEntity
        {
            Name = req.Name,
            Price = req.Price,
            Stock = req.Stock
        };
        _db.Products.Add(entity);
        await _db.SaveChangesAsync();
        return new ProductDto(entity.Id, entity.Name, entity.Price, entity.Stock);
    }

    public async Task<ProductDto?> UpdateAsync(int id, UpdateProductRequest req)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return null;
        
        p.Name = req.Name;
        p.Price = req.Price;
        p.Stock = req.Stock;
        
        await _db.SaveChangesAsync();
        return new ProductDto(p.Id, p.Name, p.Price, p.Stock);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return false;
        
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return true;
    }
}
