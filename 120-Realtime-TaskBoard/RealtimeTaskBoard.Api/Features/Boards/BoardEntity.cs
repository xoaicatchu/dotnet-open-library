namespace RealtimeTaskBoard.Api.Features.Boards;

public class BoardEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public List<Columns.ColumnEntity> Columns { get; set; } = [];
    public List<Activities.ActivityEntity> Activities { get; set; } = [];
}
