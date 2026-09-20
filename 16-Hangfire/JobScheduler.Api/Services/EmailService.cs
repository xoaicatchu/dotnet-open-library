using JobScheduler.Api.Data;

namespace JobScheduler.Api.Services;

public class EmailService : IEmailService
{
    private readonly JobAuditStore _auditStore;
    private readonly ILogger<EmailService> _logger;

    public EmailService(JobAuditStore auditStore, ILogger<EmailService> logger)
    {
        _auditStore = auditStore;
        _logger = logger;
    }

    public async Task SendWelcomeEmail(string email, string name)
    {
        _logger.LogInformation("Sending welcome email to {Name} ({Email})", name, email);
        await Task.Delay(100);
        _auditStore.RecordExecution("WelcomeEmail", $"Sent to {name} ({email})");
    }

    public async Task SendReminderEmail(string email, string message)
    {
        _logger.LogInformation("Sending reminder email to {Email}: {Message}", email, message);
        await Task.Delay(100);
        _auditStore.RecordExecution("ReminderEmail", $"Sent to {email}: {message}");
    }
}
