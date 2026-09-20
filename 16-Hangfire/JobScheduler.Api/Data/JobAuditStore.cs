using System.Collections.Concurrent;

namespace JobScheduler.Api.Data;

public class JobAuditStore
{
    private readonly ConcurrentBag<JobExecutionRecord> _executions = new();

    public void RecordExecution(string jobType, string details)
    {
        _executions.Add(new JobExecutionRecord(jobType, details, DateTime.UtcNow));
    }

    public IEnumerable<JobExecutionRecord> GetExecutions()
    {
        return _executions.OrderByDescending(e => e.ExecutedAt);
    }
}

public record JobExecutionRecord(string JobType, string Details, DateTime ExecutedAt);
