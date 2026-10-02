namespace AspireDemo.ApiService.Models;

// ── Domain models ──────────────────────────────────────────────────────────

/// <summary>
/// Task được lưu vào PostgreSQL (EF Core) và cache vào Redis.
/// Cũng được push vào Redis queue để Worker xử lý async.
/// </summary>
public class WorkTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}

public enum WorkTaskStatus { Pending, Processing, Completed, Failed }

// ── DTOs ───────────────────────────────────────────────────────────────────

public record CreateTaskRequest(string Title, string Description);

public record TaskResponse(int Id, string Title, string Description,
    string Status, DateTime CreatedAt, DateTime? ProcessedAt);

public static class TaskMapper
{
    public static TaskResponse ToResponse(this WorkTask t) =>
        new(t.Id, t.Title, t.Description, t.Status.ToString(), t.CreatedAt, t.ProcessedAt);
}
