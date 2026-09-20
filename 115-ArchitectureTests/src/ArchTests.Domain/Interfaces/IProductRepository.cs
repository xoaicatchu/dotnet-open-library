using ArchTests.Domain.Entities;

namespace ArchTests.Domain.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task AddAsync(Product product);
    Task DeleteAsync(int id);
}
