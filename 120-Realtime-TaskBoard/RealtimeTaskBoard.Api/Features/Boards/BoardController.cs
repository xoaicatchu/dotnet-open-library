using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RealtimeTaskBoard.Api.Features.Activities;
using RealtimeTaskBoard.Api.Features.Columns;
using RealtimeTaskBoard.Api.Infrastructure.Data;

namespace RealtimeTaskBoard.Api.Features.Boards;

[ApiController]
[Route("api/[controller]")]
public class BoardsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ActivityService _activityService;

    public BoardsController(AppDbContext db, ActivityService activityService)
    {
        _db = db;
        _activityService = activityService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BoardDto>>> GetAll()
    {
        var boards = await _db.Boards
            .OrderByDescending(b => b.UpdatedAt)
            .Select(b => new BoardDto(b.Id, b.Name, b.Description, b.CreatedAt, b.UpdatedAt))
            .ToListAsync();
        return Ok(boards);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BoardDetailDto>> GetById(Guid id)
    {
        var board = await _db.Boards
            .Include(b => b.Columns.OrderBy(c => c.Position))
                .ThenInclude(c => c.Tasks.OrderBy(t => t.Position))
            .FirstOrDefaultAsync(b => b.Id == id);

        if (board is null) return NotFound();

        var dto = new BoardDetailDto(
            board.Id, board.Name, board.Description, board.CreatedAt, board.UpdatedAt,
            board.Columns.Select(c => new ColumnWithTasksDto(
                c.Id, c.BoardId, c.Name, c.Position, c.Color,
                c.Tasks.Select(t => new Tasks.TaskDto(
                    t.Id, t.ColumnId, t.Title, t.Description, t.Priority,
                    t.Assignee, t.Position, t.Labels, t.DueDate, t.CreatedAt, t.UpdatedAt
                )).ToList()
            )).ToList());

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<BoardDto>> Create([FromBody] CreateBoardRequest request)
    {
        var board = new BoardEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description
        };

        // Seed default columns
        var defaults = new[] { ("Backlog", "#94a3b8"), ("In Progress", "#3b82f6"), ("Review", "#f59e0b"), ("Done", "#22c55e") };
        for (int i = 0; i < defaults.Length; i++)
        {
            board.Columns.Add(new ColumnEntity
            {
                Id = Guid.NewGuid(),
                Name = defaults[i].Item1,
                Color = defaults[i].Item2,
                Position = i
            });
        }

        _db.Boards.Add(board);
        await _db.SaveChangesAsync();

        await _activityService.LogAsync(board.Id, "System", ActivityAction.Created, "Board", board.Id, newValue: board.Name);

        var dto = new BoardDto(board.Id, board.Name, board.Description, board.CreatedAt, board.UpdatedAt);
        return CreatedAtAction(nameof(GetById), new { id = board.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BoardDto>> Update(Guid id, [FromBody] UpdateBoardRequest request)
    {
        var board = await _db.Boards.FindAsync(id);
        if (board is null) return NotFound();

        var oldName = board.Name;
        board.Name = request.Name;
        board.Description = request.Description;
        board.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _activityService.LogAsync(board.Id, "System", ActivityAction.Updated, "Board", board.Id, oldName, board.Name);

        return Ok(new BoardDto(board.Id, board.Name, board.Description, board.CreatedAt, board.UpdatedAt));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var board = await _db.Boards.FindAsync(id);
        if (board is null) return NotFound();

        _db.Boards.Remove(board);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
