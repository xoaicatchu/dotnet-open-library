using Microsoft.AspNetCore.Mvc;
using WorkerService.Api.Services;

namespace WorkerService.Api.Controllers;

public record SendEmailRequest(string To, string Subject, string Body);

[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailQueue _queue;
    
    public EmailController(IEmailQueue queue)
    {
        _queue = queue;
    }

    [HttpPost("send")] // POST /api/email/send
    public async Task<IActionResult> Send([FromBody] SendEmailRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.To))
        {
            return BadRequest("To address is required");
        }

        await _queue.EnqueueAsync(new EmailMessage(req.To, req.Subject, req.Body));
        return Accepted(new { Message = "Email queued", QueueDepth = _queue.Count });
    }
    
    [HttpGet("queue-depth")]
    public IActionResult QueueDepth() => Ok(new { Depth = _queue.Count });
}
