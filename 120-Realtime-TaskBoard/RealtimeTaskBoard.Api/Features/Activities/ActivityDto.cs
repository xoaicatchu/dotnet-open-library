namespace RealtimeTaskBoard.Api.Features.Activities;

public record ActivityDto(
    Guid Id, Guid BoardId, string ActorName, ActivityAction ActionType,
    string EntityType, Guid EntityId, string? OldValue, string? NewValue, DateTime Timestamp);
