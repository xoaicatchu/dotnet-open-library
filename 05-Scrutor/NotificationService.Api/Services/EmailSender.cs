using NotificationService.Api.Abstractions;

namespace NotificationService.Api.Services;

public class EmailSender : INotificationSender
{
    public string Channel => "email";

    public Task<NotificationResult> SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        return Task.FromResult(new NotificationResult(true, Channel, $"Email sent to {to}: {subject}"));
    }
}
