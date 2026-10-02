using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NpgsqlTypes;
using StockFlow.Api.Data;

namespace StockFlow.Api.Inventory;

public sealed class InventoryTransactionRunner(StockFlowDbContext db)
{
    public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> command, CancellationToken cancellationToken) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync<T>(async retryToken =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(retryToken);
            var result = await command(retryToken);
            await transaction.CommitAsync(retryToken);
            return result;
        }, cancellationToken);

    public async Task<IReadOnlyList<Guid>> LockProductsForUpdateAsync(IEnumerable<Guid> productIds, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Product row locks must be acquired inside the inventory transaction.");
        var orderedIds = productIds.Where(id => id != Guid.Empty).Distinct().Order().ToArray();
        if (orderedIds.Length == 0) return [];

        var connection = db.Database.GetDbConnection();
        await using DbCommand command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction.GetDbTransaction();
        command.CommandText = "SELECT \"Id\" FROM \"Products\" WHERE \"Id\" = ANY (@product_ids) ORDER BY \"Id\" FOR UPDATE";
        command.Parameters.Add(new NpgsqlParameter("product_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid) { Value = orderedIds });
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var lockedIds = new List<Guid>(orderedIds.Length);
        while (await reader.ReadAsync(cancellationToken)) lockedIds.Add(reader.GetGuid(0));
        return lockedIds;
    }
}
