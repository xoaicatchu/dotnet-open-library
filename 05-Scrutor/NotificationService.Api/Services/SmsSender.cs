using NotificationService.Api.Abstractions;

namespace NotificationService.Api.Services;

public class SmsSender : INotificationSender
{
    public string Channel => "sms";

    public Task<NotificationResult> SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        return Task.FromResult(new NotificationResult(true, Channel, $"SMS sent to {to}: {subject}"));
    }
}
