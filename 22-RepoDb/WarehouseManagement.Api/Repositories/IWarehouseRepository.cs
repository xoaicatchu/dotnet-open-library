using WarehouseManagement.Api.Entities;

namespace WarehouseManagement.Api.Repositories;

public interface IWarehouseRepository
{
    Task<IEnumerable<WarehouseItem>> GetAllAsync(string? location = null, string? search = null);
    Task<WarehouseItem?> GetByIdAsync(long id);
    Task<WarehouseItem?> GetBySkuAsync(string sku);
    Task<long> CreateAsync(WarehouseItem item);
    Task<bool> UpdateAsync(WarehouseItem item);
    Task<bool> DeleteAsync(long id);
    Task<long> UpsertAsync(WarehouseItem item);
    Task<int> BatchRestockAsync(IEnumerable<string> skus, int addedQuantity);
}
