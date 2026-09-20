using Coravel.Queuing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using TaskQueueScheduler.Api.Data;
using TaskQueueScheduler.Api.Invocables;
using TaskQueueScheduler.Api.Models;

namespace TaskQueueScheduler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly IQueue _queue;
    private readonly TaskAuditStore _store;

    public TasksController(IQueue queue, TaskAuditStore store)
    {
        _queue = queue;
        _store = store;
    }

    [HttpPost("queue")]
    public IActionResult QueueTask([FromBody] QueueTaskRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title) || req.RecordCount <= 0)
        {
            return BadRequest(new { Error = "Invalid title or negative/zero count" });
        }

        _queue.QueueInvocableWithPayload<DataProcessingInvocable, TaskPayload>(
            new TaskPayload(req.Title, req.RecordCount));

        return Accepted("/api/tasks/history", new { Message = "Task queued", req.Title });
    }

    [HttpGet("heartbeats")]
    public IActionResult GetHeartbeats()
    {
        return Ok(_store.GetHeartbeats());
    }

    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        return Ok(_store.GetHistory());
    }
}
