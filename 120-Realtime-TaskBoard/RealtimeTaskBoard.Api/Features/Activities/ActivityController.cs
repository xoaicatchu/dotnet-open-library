using Microsoft.AspNetCore.Mvc;
using RealtimeTaskBoard.Api.Infrastructure.Data;

namespace RealtimeTaskBoard.Api.Features.Activities;

[ApiController]
[Route("api/boards/{boardId:guid}/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly ActivityService _activityService;

    public ActivitiesController(ActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ActivityDto>>> Get(Guid boardId, [FromQuery] int limit = 50)
    {
        var activities = await _activityService.GetBoardActivitiesAsync(boardId, limit);
        return Ok(activities);
    }
}
