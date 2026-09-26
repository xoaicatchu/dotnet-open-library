using System.Text.Json;

namespace RealtimeTaskBoard.Api.Features.Tasks;

public class TaskEntity
{
    public Guid Id { get; set; }
    public Guid ColumnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public string? Assignee { get; set; }
    public int Position { get; set; }
    public List<string> Labels { get; set; } = [];
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Columns.ColumnEntity Column { get; set; } = null!;
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
