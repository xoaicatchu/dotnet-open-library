using System.Collections.Concurrent;

namespace TaskQueueScheduler.Api.Data;

public class TaskAuditStore
{
    private readonly ConcurrentBag<DateTime> _heartbeats = new();
    private readonly ConcurrentBag<TaskRecord> _history = new();

    public void RecordHeartbeat(DateTime timestamp)
    {
        _heartbeats.Add(timestamp);
    }

    public void RecordTask(string taskId, string taskType, string details, DateTime completedAt)
    {
        _history.Add(new TaskRecord(taskId, taskType, details, completedAt));
    }

    public IEnumerable<DateTime> GetHeartbeats() => _heartbeats.OrderByDescending(h => h);
    
    public IEnumerable<TaskRecord> GetHistory() => _history.OrderByDescending(h => h.CompletedAt);
}

public record TaskRecord(string TaskId, string TaskType, string Details, DateTime CompletedAt);
