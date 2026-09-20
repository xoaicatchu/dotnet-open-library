using AutoMapper;
using Dapper;
using EasyCaching.Core;
using LinqToDB;
using LinqToDB.Data;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using RepoDb;
using ZiggyCreatures.Caching.Fusion;
using AllInOne.Api.Data;
using AllInOne.Api.Models;

namespace AllInOne.Api.Services;

public class OrderDataService
{
    private readonly AppDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IFusionCache _fusionCache;
    private readonly IEasyCachingProvider _easyCaching;
    private readonly ResiliencePipeline _resiliencePipeline;

    public OrderDataService(
        AppDbContext dbContext,
        IMapper mapper,
        IFusionCache fusionCache,
        IEasyCachingProvider easyCaching)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _fusionCache = fusionCache;
        _easyCaching = easyCaching;

        // Polly v8 (Library 30) - Resilience Pipeline
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Constant
            })
            .Build();
    }

    // EF Core (Library 19) + AutoMapper (Library 31)
    public async Task<OrderDto?> GetOrderByIdEfAsync(int id)
    {
        var entity = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            _dbContext.Orders.Include(o => o.Items).AsNoTracking(),
            o => o.Id == id);

        return entity == null ? null : _mapper.Map<OrderDto>(entity);
    }

    // Dapper (Library 20) - High-speed dashboard query
    public async Task<List<OrderSummaryDto>> GetOrderSummariesDapperAsync()
    {
        var conn = _dbContext.Database.GetDbConnection();
        const string sql = "SELECT Id, OrderNumber, CustomerName, TotalAmount, Status FROM Orders ORDER BY Id DESC";

        var summaries = await conn.QueryAsync<OrderSummaryDto>(sql);
        return summaries.ToList();
    }

    // Mapster (Library 32) - Fast projection
    public async Task<List<OrderSummaryDto>> GetOrderSummariesMapsterAsync()
    {
        var entities = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            _dbContext.Orders.AsNoTracking());
        return entities.Adapt<List<OrderSummaryDto>>();
    }

    // FusionCache (Library 27) - Multi-level cache with anti-stampede
    public async Task<OrderDto?> GetCachedOrderAsync(int id)
    {
        return await _fusionCache.GetOrSetAsync($"order:{id}", async _ =>
        {
            return await GetOrderByIdEfAsync(id);
        }, TimeSpan.FromMinutes(5));
    }

    // EasyCaching (Library 28) - Caching abstraction
    public async Task CacheOrderSummaryAsync(int id, OrderSummaryDto summary)
    {
        await _easyCaching.SetAsync($"summary:{id}", summary, TimeSpan.FromMinutes(10));
    }

    // LinqToDB (Library 21) - Bulk insert simulation
    public async Task<int> BulkInsertOrdersLinqToDbAsync(List<OrderEntity> orders)
    {
        var conn = _dbContext.Database.GetDbConnection();
        var options = new LinqToDB.DataOptions().UseConnectionString(LinqToDB.ProviderName.SQLiteMS, conn.ConnectionString ?? "Data Source=allinone.db");
        using var dc = new LinqToDB.Data.DataConnection(options);
        var rows = await dc.BulkCopyAsync(orders);
        return (int)rows.RowsCopied;
    }

    // RepoDb (Library 22) - Fast single entity query
    public async Task<OrderEntity?> GetOrderRepoDbAsync(int id)
    {
        var conn = _dbContext.Database.GetDbConnection();
        var results = await conn.QueryAsync<OrderEntity>(id);
        return results.FirstOrDefault();
    }

    // Polly (Library 30) - Execute with resilience
    public async Task<T> ExecuteWithResilienceAsync<T>(Func<Task<T>> action)
    {
        return await _resiliencePipeline.ExecuteAsync(async _ => await action());
    }
}
