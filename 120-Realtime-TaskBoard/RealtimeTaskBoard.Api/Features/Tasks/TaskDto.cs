namespace RealtimeTaskBoard.Api.Features.Tasks;

public record TaskDto(
    Guid Id, Guid ColumnId, string Title, string? Description,
    TaskPriority Priority, string? Assignee, int Position,
    List<string> Labels, DateTime? DueDate, DateTime CreatedAt, DateTime UpdatedAt);

public record CreateTaskRequest(string Title, string? Description, TaskPriority? Priority, string? Assignee, List<string>? Labels, DateTime? DueDate);

public record UpdateTaskRequest(string? Title, string? Description, TaskPriority? Priority, string? Assignee, List<string>? Labels, DateTime? DueDate);

public record MoveTaskRequest(Guid TargetColumnId, int NewPosition);
