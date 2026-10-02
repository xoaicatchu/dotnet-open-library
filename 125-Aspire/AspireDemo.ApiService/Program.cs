using AspireDemo.ApiService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. ServiceDefaults: OpenTelemetry + health checks + service discovery (một dòng thay hàng chục)
builder.AddServiceDefaults();

// 2. Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "125 – .NET Aspire Demo: ApiService",
        Version = "v1",
        Description = """
            ApiService trong .NET Aspire solution.
            
            Khi chạy qua AppHost (dotnet run --project AspireDemo.AppHost):
            - Aspire inject ConnectionStrings__redis và ConnectionStrings__aspiredb tự động
            - Aspire Dashboard mở tại https://localhost:15888
            
            Khi chạy standalone (dotnet run --project AspireDemo.ApiService):
            - Dùng SQLite in-memory
            - Task queue dùng in-memory (không cần Redis)
            """
    });
});

// 3. Redis integration (nếu có connection string từ Aspire AppHost)
var hasRedis = !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("redis"));
if (hasRedis)
{
    builder.AddRedisClient("redis");
    builder.Services.AddScoped<ITaskQueue, RedisTaskQueue>();
}
else
{
    // Standalone: in-memory queue (không cần Redis)
    builder.Services.AddScoped<ITaskQueue, InMemoryTaskQueue>();
}

// 4. EF Core – SQLite standalone, PostgreSQL qua Aspire
var dbConn = builder.Configuration.GetConnectionString("aspiredb");
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(string.IsNullOrEmpty(dbConn) ? "Data Source=aspire-demo.db" : dbConn));

var app = builder.Build();

// 5. Auto-migrate DB on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// 6. Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.MapDefaultEndpoints(); // /health, /alive

app.Run();

public partial class Program { }

/// <summary>In-memory task queue (standalone / test mode — no Redis required).</summary>
public sealed class InMemoryTaskQueue : ITaskQueue
{
    private static readonly Queue<int> _queue = new();
    public Task EnqueueAsync(int taskId, CancellationToken ct = default) { lock (_queue) _queue.Enqueue(taskId); return Task.CompletedTask; }
    public Task<int?> DequeueAsync(CancellationToken ct = default) { lock (_queue) return Task.FromResult(_queue.Count > 0 ? (int?)_queue.Dequeue() : null); }
}
