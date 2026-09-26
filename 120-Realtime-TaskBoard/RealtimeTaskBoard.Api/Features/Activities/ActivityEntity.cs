namespace RealtimeTaskBoard.Api.Features.Activities;

public class ActivityEntity
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string ActorName { get; set; } = "System";
    public ActivityAction ActionType { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public Boards.BoardEntity Board { get; set; } = null!;
}

public enum ActivityAction
{
    Created,
    Updated,
    Deleted,
    Moved
}
