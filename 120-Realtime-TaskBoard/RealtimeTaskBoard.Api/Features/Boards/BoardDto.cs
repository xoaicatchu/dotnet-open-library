namespace RealtimeTaskBoard.Api.Features.Boards;

public record BoardDto(Guid Id, string Name, string? Description, DateTime CreatedAt, DateTime UpdatedAt);

public record CreateBoardRequest(string Name, string? Description);

public record UpdateBoardRequest(string Name, string? Description);

public record BoardDetailDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<Columns.ColumnWithTasksDto> Columns);
