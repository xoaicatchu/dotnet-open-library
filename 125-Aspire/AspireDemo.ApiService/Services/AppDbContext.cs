using AspireDemo.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace AspireDemo.ApiService.Services;

/// <summary>
/// EF Core DbContext — connection string được inject tự động bởi Aspire
/// thông qua environment variable ConnectionStrings__aspiredb.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<WorkTask> Tasks => Set<WorkTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkTask>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(200).IsRequired();
            e.Property(t => t.Description).HasMaxLength(2000);
            e.Property(t => t.Status).HasConversion<string>();
        });
    }
}

/// <summary>
/// Task queue service: đẩy task ID vào Redis list để Worker lấy ra xử lý.
/// Dùng Redis LPUSH / BRPOP pattern (reliable queue).
/// </summary>
public interface ITaskQueue
{
    Task EnqueueAsync(int taskId, CancellationToken ct = default);
    Task<int?> DequeueAsync(CancellationToken ct = default);
}

public sealed class RedisTaskQueue : ITaskQueue
{
    private const string QueueKey = "tasks:pending";
    private readonly StackExchange.Redis.IDatabase _db;
    private readonly ILogger<RedisTaskQueue> _logger;

    public RedisTaskQueue(
        StackExchange.Redis.IConnectionMultiplexer mux,
        ILogger<RedisTaskQueue> logger)
    {
        _db = mux.GetDatabase();
        _logger = logger;
    }

    public async Task EnqueueAsync(int taskId, CancellationToken ct = default)
    {
        await _db.ListLeftPushAsync(QueueKey, taskId);
        _logger.LogInformation("Enqueued task {TaskId} to Redis queue", taskId);
    }

    public async Task<int?> DequeueAsync(CancellationToken ct = default)
    {
        var value = await _db.ListRightPopAsync(QueueKey);
        if (value.IsNullOrEmpty) return null;
        return (int)value;
    }
}
