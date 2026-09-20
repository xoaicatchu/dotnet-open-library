using NotificationService.Api.Abstractions;

namespace NotificationService.Api.Decorators;

public class LoggingNotificationSender : INotificationSender
{
    private readonly INotificationSender _inner;
    private readonly ILogger<LoggingNotificationSender> _logger;

    public LoggingNotificationSender(INotificationSender inner, ILogger<LoggingNotificationSender> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public string Channel => _inner.Channel;

    public async Task<NotificationResult> SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation("Sending {Channel} notification to {To}...", Channel, to);
        var result = await _inner.SendAsync(to, subject, body, ct);
        _logger.LogInformation("Sent {Channel} notification to {To} with result: {Success}", Channel, to, result.Success);
        return result;
    }
}
