using NotificationService.Api.Abstractions;

namespace NotificationService.Api.Services;

public class PushSender : INotificationSender
{
    public string Channel => "push";

    public Task<NotificationResult> SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        return Task.FromResult(new NotificationResult(true, Channel, $"Push sent to {to}: {subject}"));
    }
}
