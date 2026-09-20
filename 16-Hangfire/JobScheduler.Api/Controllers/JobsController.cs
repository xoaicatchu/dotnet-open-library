using Hangfire;
using JobScheduler.Api.Data;
using JobScheduler.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobScheduler.Api.Controllers;

public record WelcomeEmailRequest(string Email, string Name);
public record DelayedReminderRequest(string Email, string Message, int DelaySeconds);
public record DailyReportRequest(string ReportType);

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly IRecurringJobManager _recurringJobs;
    private readonly JobAuditStore _auditStore;

    public JobsController(
        IBackgroundJobClient backgroundJobs,
        IRecurringJobManager recurringJobs,
        JobAuditStore auditStore)
    {
        _backgroundJobs = backgroundJobs;
        _recurringJobs = recurringJobs;
        _auditStore = auditStore;
    }

    [HttpPost("welcome-email")]
    public IActionResult WelcomeEmail([FromBody] WelcomeEmailRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Email and Name are required.");

        var jobId = _backgroundJobs.Enqueue<IEmailService>(svc => svc.SendWelcomeEmail(req.Email, req.Name));
        return Accepted("/api/jobs/history", new { JobId = jobId, Type = "FireAndForget" });
    }

    [HttpPost("delayed-reminder")]
    public IActionResult DelayedReminder([FromBody] DelayedReminderRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Message))
            return BadRequest("Email and Message are required.");

        var delay = TimeSpan.FromSeconds(req.DelaySeconds > 0 ? req.DelaySeconds : 2);
        var jobId = _backgroundJobs.Schedule<IEmailService>(
            svc => svc.SendReminderEmail(req.Email, req.Message), delay);
        return Accepted("/api/jobs/history", new { JobId = jobId, Type = "Delayed" });
    }

    [HttpPost("recurring/daily-report")]
    public IActionResult DailyReport([FromBody] DailyReportRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.ReportType))
            return BadRequest("ReportType is required.");

        _recurringJobs.AddOrUpdate<IReportService>("daily-sales-report",
            svc => svc.GenerateDailyReport(req.ReportType), Cron.Daily);
        
        _recurringJobs.Trigger("daily-sales-report");
        return Ok(new { Message = "Recurring job configured and triggered", JobId = "daily-sales-report" });
    }

    [HttpGet("history")]
    public IActionResult GetHistory()
    {
        return Ok(_auditStore.GetExecutions());
    }
}
