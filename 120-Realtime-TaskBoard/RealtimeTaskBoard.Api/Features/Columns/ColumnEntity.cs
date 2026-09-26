namespace RealtimeTaskBoard.Api.Features.Columns;

public class ColumnEntity
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public string Color { get; set; } = "#e2e8f0";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Boards.BoardEntity Board { get; set; } = null!;
    public List<Tasks.TaskEntity> Tasks { get; set; } = [];
}
