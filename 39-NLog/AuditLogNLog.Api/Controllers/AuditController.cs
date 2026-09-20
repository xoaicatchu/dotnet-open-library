using Microsoft.AspNetCore.Mvc;
using AuditLogNLog.Api.Models;

namespace AuditLogNLog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly ILogger<AuditController> _logger;

    public AuditController(ILogger<AuditController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a user login audit event.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuditResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RecordLogin([FromBody] LoginAuditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "Username is required." });
        }

        var eventId = $"AUD-LOG-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        if (request.Success)
        {
            _logger.LogInformation("Audit [{EventId}]: User '{Username}' logged in successfully from IP '{IpAddress}'",
                eventId, request.Username, request.IpAddress);
        }
        else
        {
            _logger.LogWarning("Audit [{EventId}]: Failed login attempt for user '{Username}' from IP '{IpAddress}'",
                eventId, request.Username, request.IpAddress);
        }

        return Ok(new AuditResponse(
            EventId: eventId,
            Status: request.Success ? "LoginSuccess" : "LoginFailure",
            Timestamp: DateTime.UtcNow
        ));
    }

    /// <summary>
    /// Records an administrative or sensitive business action.
    /// </summary>
    [HttpPost("action")]
    [ProducesResponseType(typeof(AuditResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RecordAction([FromBody] ActionAuditRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.ActionName))
        {
            return BadRequest(new { message = "Username and ActionName are required." });
        }

        var eventId = $"AUD-ACT-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";

        _logger.LogInformation("Audit [{EventId}]: User '{Username}' executed action '{ActionName}' on resource '{ResourceId}'. Details: {Details}",
            eventId, request.Username, request.ActionName, request.ResourceId, request.Details);

        return Ok(new AuditResponse(
            EventId: eventId,
            Status: "ActionRecorded",
            Timestamp: DateTime.UtcNow
        ));
    }

    /// <summary>
    /// Auditing system heartbeat and diagnostic status check.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(AuditResponse), StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        var eventId = $"AUD-HLT-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        _logger.LogInformation("Audit [{EventId}]: Audit subsystem health check executed successfully.", eventId);

        return Ok(new AuditResponse(
            EventId: eventId,
            Status: "Healthy",
            Timestamp: DateTime.UtcNow
        ));
    }
}
