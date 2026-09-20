namespace CronScheduler.Api.Data;
using CronScheduler.Api.Models;

public class MetricAuditStore
{
    private readonly List<MetricRecord> _metrics = new();
    private readonly object _lock = new();

    public void Add(MetricRecord record)
    {
        lock (_lock)
        {
            _metrics.Add(record);
        }
    }

    public IReadOnlyList<MetricRecord> GetAll()
    {
        lock (_lock)
        {
            return _metrics.ToList();
        }
    }
}
