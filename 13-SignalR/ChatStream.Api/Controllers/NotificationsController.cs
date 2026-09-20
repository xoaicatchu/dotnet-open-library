using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatStream.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<ChatHub, IChatClient> _hubContext;

    public NotificationsController(IHubContext<ChatHub, IChatClient> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest req)
    {
        await _hubContext.Clients.All.ReceiveNotification(req.Message, DateTime.UtcNow);
        return Ok();
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { Status = "Active", Timestamp = DateTime.UtcNow });
    }
}
