using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealtimeTaskBoard.Api.Features.Activities;
using RealtimeTaskBoard.Api.Infrastructure.Data;

namespace RealtimeTaskBoard.Api.Features.Columns;

[ApiController]
[Route("api")]
public class ColumnsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHubContext<Boards.BoardHub> _boardHub;
    private readonly ActivityService _activityService;

    public ColumnsController(AppDbContext db, IHubContext<Boards.BoardHub> boardHub, ActivityService activityService)
    {
        _db = db;
        _boardHub = boardHub;
        _activityService = activityService;
    }

    [HttpGet("boards/{boardId:guid}/columns")]
    public async Task<ActionResult<List<ColumnDto>>> GetByBoard(Guid boardId)
    {
        var columns = await _db.Columns
            .Where(c => c.BoardId == boardId)
            .OrderBy(c => c.Position)
            .Select(c => new ColumnDto(c.Id, c.BoardId, c.Name, c.Position, c.Color))
            .ToListAsync();
        return Ok(columns);
    }

    [HttpPost("boards/{boardId:guid}/columns")]
    public async Task<ActionResult<ColumnDto>> Create(Guid boardId, [FromBody] CreateColumnRequest request)
    {
        var boardExists = await _db.Boards.AnyAsync(b => b.Id == boardId);
        if (!boardExists) return NotFound("Board not found");

        var maxPos = await _db.Columns
            .Where(c => c.BoardId == boardId)
            .MaxAsync(c => (int?)c.Position) ?? -1;

        var column = new ColumnEntity
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Name = request.Name,
            Color = request.Color ?? "#e2e8f0",
            Position = maxPos + 1
        };

        _db.Columns.Add(column);
        await _db.SaveChangesAsync();

        var dto = new ColumnDto(column.Id, column.BoardId, column.Name, column.Position, column.Color);
        await _boardHub.Clients.Group($"board-{boardId}").SendAsync("ColumnAdded", dto);
        await _activityService.LogAsync(boardId, "System", ActivityAction.Created, "Column", column.Id, newValue: column.Name);

        return Created($"/api/columns/{column.Id}", dto);
    }

    [HttpPut("columns/{id:guid}")]
    public async Task<ActionResult<ColumnDto>> Update(Guid id, [FromBody] UpdateColumnRequest request)
    {
        var column = await _db.Columns.FindAsync(id);
        if (column is null) return NotFound();

        var oldName = column.Name;
        column.Name = request.Name;
        if (request.Color is not null) column.Color = request.Color;
        await _db.SaveChangesAsync();

        var dto = new ColumnDto(column.Id, column.BoardId, column.Name, column.Position, column.Color);
        await _boardHub.Clients.Group($"board-{column.BoardId}").SendAsync("BoardUpdated", dto);
        await _activityService.LogAsync(column.BoardId, "System", ActivityAction.Updated, "Column", column.Id, oldName, column.Name);

        return Ok(dto);
    }

    [HttpPut("columns/reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderColumnsRequest request)
    {
        var columns = await _db.Columns
            .Where(c => request.ColumnIds.Contains(c.Id))
            .ToListAsync();

        if (columns.Count == 0) return NotFound();

        var boardId = columns.First().BoardId;

        for (int i = 0; i < request.ColumnIds.Count; i++)
        {
            var col = columns.FirstOrDefault(c => c.Id == request.ColumnIds[i]);
            if (col is not null) col.Position = i;
        }

        await _db.SaveChangesAsync();

        await _boardHub.Clients.Group($"board-{boardId}").SendAsync("ColumnMoved", request.ColumnIds);
        await _activityService.LogAsync(boardId, "System", ActivityAction.Moved, "Column", boardId, newValue: "Columns reordered");

        return NoContent();
    }

    [HttpDelete("columns/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var column = await _db.Columns.FindAsync(id);
        if (column is null) return NotFound();

        var boardId = column.BoardId;
        var columnName = column.Name;

        _db.Columns.Remove(column);
        await _db.SaveChangesAsync();

        await _boardHub.Clients.Group($"board-{boardId}").SendAsync("ColumnDeleted", id);
        await _activityService.LogAsync(boardId, "System", ActivityAction.Deleted, "Column", id, oldValue: columnName);

        return NoContent();
    }
}
