using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkerService.Api.Data;

namespace WorkerService.Api.Controllers;

public record JobDto(int Id, string Type, string Status, DateTime CreatedAt, DateTime? CompletedAt, string? Result);

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly AppDbContext _db;
    
    public JobsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet] // GET /api/jobs - list all jobs
    public async Task<ActionResult<List<JobDto>>> GetAll()
        => Ok(await _db.Jobs.AsNoTracking().OrderByDescending(j => j.CreatedAt).Take(50)
            .Select(j => new JobDto(j.Id, j.Type, j.Status.ToString(), j.CreatedAt, j.CompletedAt, j.Result))
            .ToListAsync());
    
    [HttpGet("{id}")] // GET /api/jobs/{id}
    public async Task<ActionResult<JobDto>> GetById(int id)
    {
        var job = await _db.Jobs.FindAsync(id);
        return job == null ? NotFound() : Ok(new JobDto(job.Id, job.Type, job.Status.ToString(), job.CreatedAt, job.CompletedAt, job.Result));
    }
    
    [HttpGet("stats")] // GET /api/jobs/stats
    public async Task<IActionResult> Stats()
    {
        var stats = await _db.Jobs.GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        return Ok(stats);
    }
}
