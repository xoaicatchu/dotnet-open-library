using ArchTests.Application.Dtos;
using ArchTests.Domain.Entities;
using ArchTests.Domain.Interfaces;

namespace ArchTests.Application.Services;

public class ProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _repository.GetAllAsync();
        return products.Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price });
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var p = await _repository.GetByIdAsync(id);
        if (p == null) return null;
        return new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price };
    }

    public async Task AddAsync(ProductDto dto)
    {
        var product = new Product { Id = dto.Id, Name = dto.Name, Price = dto.Price };
        await _repository.AddAsync(product);
    }
    
    public async Task DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
    }
}
