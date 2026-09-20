using NotificationService.Api.Abstractions;

namespace NotificationService.Api.Decorators;

public class RetryNotificationSender : INotificationSender
{
    private readonly INotificationSender _inner;
    private readonly ILogger<RetryNotificationSender> _logger;

    public RetryNotificationSender(INotificationSender inner, ILogger<RetryNotificationSender> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public string Channel => _inner.Channel;

    public async Task<NotificationResult> SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        try
        {
            return await _inner.SendAsync(to, subject, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send {Channel} notification. Retrying...", Channel);
            return await _inner.SendAsync(to, subject, body, ct);
        }
    }
}
