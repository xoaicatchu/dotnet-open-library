namespace NotificationService.Api.Abstractions;

public interface INotificationSender
{
    string Channel { get; }
    Task<NotificationResult> SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

public record NotificationResult(bool Success, string Channel, string Message);
