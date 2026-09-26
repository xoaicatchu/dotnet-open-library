using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealtimeTaskBoard.Api.Infrastructure.Data;

namespace RealtimeTaskBoard.Api.Features.Activities;

public class ActivityService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<ActivityHub> _activityHub;

    public ActivityService(AppDbContext db, IHubContext<ActivityHub> activityHub)
    {
        _db = db;
        _activityHub = activityHub;
    }

    public async Task LogAsync(Guid boardId, string actor, ActivityAction action,
        string entityType, Guid entityId, string? oldValue = null, string? newValue = null)
    {
        var activity = new ActivityEntity
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            ActorName = actor,
            ActionType = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldValue,
            NewValue = newValue,
            Timestamp = DateTime.UtcNow
        };

        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var dto = new ActivityDto(
            activity.Id, activity.BoardId, activity.ActorName,
            activity.ActionType, activity.EntityType, activity.EntityId,
            activity.OldValue, activity.NewValue, activity.Timestamp);

        await _activityHub.Clients.Group($"board-{boardId}")
            .SendAsync("ActivityLogged", dto);
    }

    public async Task<List<ActivityDto>> GetBoardActivitiesAsync(Guid boardId, int limit = 50)
    {
        return await _db.Activities
            .Where(a => a.BoardId == boardId)
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .Select(a => new ActivityDto(
                a.Id, a.BoardId, a.ActorName, a.ActionType,
                a.EntityType, a.EntityId, a.OldValue, a.NewValue, a.Timestamp))
            .ToListAsync();
    }
}
