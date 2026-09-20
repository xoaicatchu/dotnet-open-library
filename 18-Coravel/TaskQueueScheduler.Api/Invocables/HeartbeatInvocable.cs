using Coravel.Invocable;
using TaskQueueScheduler.Api.Data;

namespace TaskQueueScheduler.Api.Invocables;

public class HeartbeatInvocable : IInvocable
{
    private readonly TaskAuditStore _store;
    private readonly ILogger<HeartbeatInvocable> _logger;

    public HeartbeatInvocable(TaskAuditStore store, ILogger<HeartbeatInvocable> logger)
    {
        _store = store;
        _logger = logger;
    }

    public Task Invoke()
    {
        _logger.LogInformation("Heartbeat invoked at {Time}", DateTime.UtcNow);
        _store.RecordHeartbeat(DateTime.UtcNow);
        return Task.CompletedTask;
    }
}
