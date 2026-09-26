namespace RealtimeTaskBoard.Api.Features.Columns;

public record ColumnDto(Guid Id, Guid BoardId, string Name, int Position, string Color);

public record ColumnWithTasksDto(Guid Id, Guid BoardId, string Name, int Position, string Color, List<Tasks.TaskDto> Tasks);

public record CreateColumnRequest(string Name, string? Color);

public record UpdateColumnRequest(string Name, string? Color);

public record ReorderColumnsRequest(List<Guid> ColumnIds);
