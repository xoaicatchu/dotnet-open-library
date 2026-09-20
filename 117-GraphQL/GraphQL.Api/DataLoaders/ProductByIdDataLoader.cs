using GraphQL.Api.Data;
using GraphQL.Api.Entities;
using GreenDonut;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GraphQL.Api.DataLoaders;

public class ProductByIdDataLoader : BatchDataLoader<int, ProductEntity?>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    
    public ProductByIdDataLoader(
        IDbContextFactory<AppDbContext> dbFactory,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options) : base(batchScheduler, options)
    {
        _dbFactory = dbFactory;
    }
    
    protected override async Task<IReadOnlyDictionary<int, ProductEntity?>> LoadBatchAsync(
        IReadOnlyList<int> keys, CancellationToken ct)
    {
        using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Products
            .Where(p => keys.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => (ProductEntity?)p, ct);
    }
}
