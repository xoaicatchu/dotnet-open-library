using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Abstractions;

namespace NotificationService.Api.Controllers;

public record SendNotificationRequest(string Channel, string To, string Subject, string Body);
public record BroadcastNotificationRequest(string To, string Subject, string Body);

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly IEnumerable<IMessageFormatter> _formatters;

    public NotificationsController(IEnumerable<INotificationSender> senders, IEnumerable<IMessageFormatter> formatters)
    {
        _senders = senders;
        _formatters = formatters;
    }

    [HttpGet("channels")]
    public IActionResult GetChannels()
    {
        return Ok(_senders.Select(s => s.Channel).Distinct());
    }

    [HttpGet("formatters")]
    public IActionResult GetFormatters()
    {
        return Ok(_formatters.Select(f => f.Format).Distinct());
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request, CancellationToken ct)
    {
        var sender = _senders.FirstOrDefault(s => s.Channel.Equals(request.Channel, StringComparison.OrdinalIgnoreCase));
        if (sender == null) return BadRequest($"Unknown channel: {request.Channel}");

        var result = await sender.SendAsync(request.To, request.Subject, request.Body, ct);
        return Ok(result);
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastNotificationRequest request, CancellationToken ct)
    {
        var results = new List<NotificationResult>();
        foreach (var sender in _senders)
        {
            results.Add(await sender.SendAsync(request.To, request.Subject, request.Body, ct));
        }
        return Ok(results);
    }
}
