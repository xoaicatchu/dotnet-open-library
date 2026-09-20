using System.Text.Json.Serialization;

namespace NswagToolchain.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TaskStatus
{
    Backlog,
    InProgress,
    Review,
    Done
}

public record ProjectTask(
    Guid Id,
    string Title,
    string Description,
    TaskPriority Priority,
    TaskStatus Status,
    string AssigneeEmail,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateTaskRequest(
    string Title,
    string Description,
    TaskPriority Priority = TaskPriority.Medium,
    string AssigneeEmail = "",
    DateTime? DueDate = null
);

public record UpdateTaskStatusRequest(
    TaskStatus Status
);

public record GeneratedClientResponse(
    string Language,
    string Code,
    DateTime GeneratedAt
);
