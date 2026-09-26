using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealtimeTaskBoard.Api.Features.Activities;
using RealtimeTaskBoard.Api.Infrastructure.Data;

namespace RealtimeTaskBoard.Api.Features.Tasks;

[ApiController]
[Route("api")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<TaskHub> _taskHub;
    private readonly ActivityService _activityService;

    public TasksController(AppDbContext db, IHubContext<TaskHub> taskHub, ActivityService activityService)
    {
        _db = db;
        _taskHub = taskHub;
        _activityService = activityService;
    }

    [HttpGet("columns/{columnId:guid}/tasks")]
    public async Task<ActionResult<List<TaskDto>>> GetByColumn(Guid columnId)
    {
        var tasks = await _db.Tasks
            .Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.Position)
            .Select(t => new TaskDto(t.Id, t.ColumnId, t.Title, t.Description,
                t.Priority, t.Assignee, t.Position, t.Labels, t.DueDate, t.CreatedAt, t.UpdatedAt))
            .ToListAsync();
        return Ok(tasks);
    }

    [HttpPost("columns/{columnId:guid}/tasks")]
    public async Task<ActionResult<TaskDto>> Create(Guid columnId, [FromBody] CreateTaskRequest request)
    {
        var column = await _db.Columns.Include(c => c.Board).FirstOrDefaultAsync(c => c.Id == columnId);
        if (column is null) return NotFound("Column not found");

        var maxPos = await _db.Tasks
            .Where(t => t.ColumnId == columnId)
            .MaxAsync(t => (int?)t.Position) ?? -1;

        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            ColumnId = columnId,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority ?? TaskPriority.Medium,
            Assignee = request.Assignee,
            Position = maxPos + 1,
            Labels = request.Labels ?? [],
            DueDate = request.DueDate
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        var dto = new TaskDto(task.Id, task.ColumnId, task.Title, task.Description,
            task.Priority, task.Assignee, task.Position, task.Labels, task.DueDate, task.CreatedAt, task.UpdatedAt);

        await _taskHub.Clients.Group($"board-{column.BoardId}").SendAsync("TaskCreated", dto);
        await _activityService.LogAsync(column.BoardId, "System", ActivityAction.Created, "Task", task.Id, newValue: task.Title);

        return Created($"/api/tasks/{task.Id}", dto);
    }

    [HttpPut("tasks/{id:guid}")]
    public async Task<ActionResult<TaskDto>> Update(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _db.Tasks.Include(t => t.Column).FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound();

        var oldTitle = task.Title;
        if (request.Title is not null) task.Title = request.Title;
        if (request.Description is not null) task.Description = request.Description;
        if (request.Priority is not null) task.Priority = request.Priority.Value;
        if (request.Assignee is not null) task.Assignee = request.Assignee;
        if (request.Labels is not null) task.Labels = request.Labels;
        if (request.DueDate is not null) task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var dto = new TaskDto(task.Id, task.ColumnId, task.Title, task.Description,
            task.Priority, task.Assignee, task.Position, task.Labels, task.DueDate, task.CreatedAt, task.UpdatedAt);

        await _taskHub.Clients.Group($"board-{task.Column.BoardId}").SendAsync("TaskUpdated", dto);
        await _activityService.LogAsync(task.Column.BoardId, "System", ActivityAction.Updated, "Task", task.Id, oldTitle, task.Title);

        return Ok(dto);
    }

    [HttpPut("tasks/{id:guid}/move")]
    public async Task<ActionResult<TaskDto>> Move(Guid id, [FromBody] MoveTaskRequest request)
    {
        var task = await _db.Tasks.Include(t => t.Column).FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound();

        var targetColumn = await _db.Columns.FindAsync(request.TargetColumnId);
        if (targetColumn is null) return NotFound("Target column not found");

        var boardId = task.Column.BoardId;
        var oldColumnId = task.ColumnId;
        var isSameColumn = request.TargetColumnId == oldColumnId;

        if (isSameColumn)
        {
            var columnTasks = await _db.Tasks
                .Where(t => t.ColumnId == oldColumnId)
                .OrderBy(t => t.Position)
                .ToListAsync();

            columnTasks.RemoveAll(t => t.Id == id);
            var targetIndex = Math.Clamp(request.NewPosition, 0, columnTasks.Count);
            columnTasks.Insert(targetIndex, task);

            for (int i = 0; i < columnTasks.Count; i++)
            {
                columnTasks[i].Position = i;
            }
        }
        else
        {
            // Normalize old column
            var sourceTasks = await _db.Tasks
                .Where(t => t.ColumnId == oldColumnId && t.Id != id)
                .OrderBy(t => t.Position)
                .ToListAsync();

            for (int i = 0; i < sourceTasks.Count; i++)
            {
                sourceTasks[i].Position = i;
            }

            // Insert into target column
            var targetTasks = await _db.Tasks
                .Where(t => t.ColumnId == request.TargetColumnId)
                .OrderBy(t => t.Position)
                .ToListAsync();

            var targetIndex = Math.Clamp(request.NewPosition, 0, targetTasks.Count);
            task.ColumnId = request.TargetColumnId;
            targetTasks.Insert(targetIndex, task);

            for (int i = 0; i < targetTasks.Count; i++)
            {
                targetTasks[i].Position = i;
            }
        }

        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var dto = new TaskDto(task.Id, task.ColumnId, task.Title, task.Description,
            task.Priority, task.Assignee, task.Position, task.Labels, task.DueDate, task.CreatedAt, task.UpdatedAt);

        await _taskHub.Clients.Group($"board-{boardId}").SendAsync("TaskMoved", new { Task = dto, OldColumnId = oldColumnId });
        await _activityService.LogAsync(boardId, "System", ActivityAction.Moved, "Task", task.Id,
            oldValue: $"Column {oldColumnId}", newValue: $"Column {request.TargetColumnId}");

        return Ok(dto);
    }

    [HttpDelete("tasks/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _db.Tasks.Include(t => t.Column).FirstOrDefaultAsync(t => t.Id == id);
        if (task is null) return NotFound();

        var boardId = task.Column.BoardId;
        var taskTitle = task.Title;

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();

        await _taskHub.Clients.Group($"board-{boardId}").SendAsync("TaskDeleted", id);
        await _activityService.LogAsync(boardId, "System", ActivityAction.Deleted, "Task", id, oldValue: taskTitle);

        return NoContent();
    }
}
