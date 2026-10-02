using AspireDemo.ApiService.Models;
using AspireDemo.ApiService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspireDemo.ApiService.Controllers;

/// <summary>
/// CRUD API cho WorkTask.
/// Demo Aspire: Postgres/SQLite (EF Core), Redis queue (khi Aspire inject), health checks.
/// </summary>
[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITaskQueue _queue;
    private readonly ILogger<TasksController> _logger;

    public TasksController(AppDbContext db, ITaskQueue queue, ILogger<TasksController> logger)
    {
        _db = db;
        _queue = queue;
        _logger = logger;
    }

    /// <summary>Lấy tất cả tasks.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TaskResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var tasks = await _db.Tasks.AsNoTracking().OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
        return Ok(tasks.Select(t => t.ToResponse()));
    }

    /// <summary>Lấy task theo ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync([id], ct);
        return task is null ? NotFound(new { error = $"Task {id} not found." }) : Ok(task.ToResponse());
    }

    /// <summary>
    /// Tạo task mới:
    ///   1. Lưu vào DB (SQLite standalone / Postgres qua Aspire)
    ///   2. Push ID vào queue → Worker xử lý async
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Title là bắt buộc." });

        var task = new WorkTask { Title = request.Title, Description = request.Description };
        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        await _queue.EnqueueAsync(task.Id, ct);

        _logger.LogInformation("Created task {Id}: '{Title}' — enqueued for processing", task.Id, task.Title);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task.ToResponse());
    }

    /// <summary>Xóa task.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync([id], ct);
        if (task is null) return NotFound();

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

/// <summary>Thông tin về Aspire infrastructure được inject.</summary>
[ApiController]
[Route("api/info")]
public class InfoController : ControllerBase
{
    private readonly IConfiguration _config;

    public InfoController(IConfiguration config) => _config = config;

    [HttpGet]
    public IActionResult GetInfo() => Ok(new
    {
        service = "AspireDemo.ApiService",
        dotnet = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        mode = string.IsNullOrEmpty(_config.GetConnectionString("redis")) ? "standalone (SQLite + InMemoryQueue)" : "aspire (Postgres + Redis)",
        aspireFeatures = new[]
        {
            "Service Discovery: http://apiservice → dynamic port resolution",
            "OpenTelemetry traces + metrics → Aspire Dashboard :15888",
            "Health checks at /health and /alive",
            "Redis task queue → Worker async processing (when Aspire mode)",
            "EF Core with SQLite (standalone) or PostgreSQL (Aspire mode)"
        },
        redisConfigured = !string.IsNullOrEmpty(_config.GetConnectionString("redis")),
        postgresConfigured = !string.IsNullOrEmpty(_config.GetConnectionString("aspiredb"))
    });
}
