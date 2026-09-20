using ProductCatalog.Api.Entities;

namespace ProductCatalog.Api.Repositories;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(string? search = null, int? categoryId = null);
    Task<Product?> GetByIdAsync(int id);
    Task<int> CreateAsync(Product product);
    Task<bool> UpdateAsync(Product product);
    Task<bool> DeleteAsync(int id);
    Task<int> ApplyBatchDiscountAsync(decimal discountPercentage, IEnumerable<int> productIds);
    Task<bool> CategoryExistsAsync(int categoryId);
}
