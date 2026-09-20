using RepoDb;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Entities;

namespace WarehouseManagement.Api.Repositories;

public class WarehouseRepository : IWarehouseRepository
{
    private readonly IDbConnectionFactory _factory;

    public WarehouseRepository(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<WarehouseItem>> GetAllAsync(string? location = null, string? search = null)
    {
        using var connection = _factory.CreateConnection();

        if (!string.IsNullOrWhiteSpace(location))
        {
            return await connection.QueryAsync<WarehouseItem>(e => e.Location == location);
        }

        var items = await connection.QueryAllAsync<WarehouseItem>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            return items.Where(i => i.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                                    i.Sku.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return items;
    }

    public async Task<WarehouseItem?> GetByIdAsync(long id)
    {
        using var connection = _factory.CreateConnection();
        var items = await connection.QueryAsync<WarehouseItem>(e => e.Id == id);
        return items.FirstOrDefault();
    }

    public async Task<WarehouseItem?> GetBySkuAsync(string sku)
    {
        using var connection = _factory.CreateConnection();
        var items = await connection.QueryAsync<WarehouseItem>(e => e.Sku == sku);
        return items.FirstOrDefault();
    }

    public async Task<long> CreateAsync(WarehouseItem item)
    {
        using var connection = _factory.CreateConnection();
        var id = await connection.InsertAsync<WarehouseItem, long>(item);
        return id;
    }

    public async Task<bool> UpdateAsync(WarehouseItem item)
    {
        using var connection = _factory.CreateConnection();
        var affected = await connection.UpdateAsync(item);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _factory.CreateConnection();
        var affected = await connection.DeleteAsync<WarehouseItem>(e => e.Id == id);
        return affected > 0;
    }

    public async Task<long> UpsertAsync(WarehouseItem item)
    {
        using var connection = _factory.CreateConnection();
        var existing = (await connection.QueryAsync<WarehouseItem>(e => e.Sku == item.Sku)).FirstOrDefault();
        if (existing != null)
        {
            existing.Name = item.Name;
            existing.Location = item.Location;
            existing.Quantity = item.Quantity;
            existing.UnitCost = item.UnitCost;
            existing.LastRestockedAt = DateTime.UtcNow;
            await connection.UpdateAsync(existing);
            return existing.Id;
        }
        else
        {
            return await connection.InsertAsync<WarehouseItem, long>(item);
        }
    }

    public async Task<int> BatchRestockAsync(IEnumerable<string> skus, int addedQuantity)
    {
        using var connection = _factory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var skuList = skus.ToList();
            var items = (await connection.QueryAllAsync<WarehouseItem>(transaction: transaction))
                .Where(i => skuList.Contains(i.Sku))
                .ToList();

            foreach (var item in items)
            {
                item.Quantity += addedQuantity;
                item.LastRestockedAt = DateTime.UtcNow;
            }

            var affected = await connection.UpdateAllAsync(items, transaction: transaction);
            transaction.Commit();
            return affected;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
