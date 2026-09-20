using CronScheduler.Api.Data;
using CronScheduler.Api.Models;
using Quartz;

namespace CronScheduler.Api.Jobs;

public class MetricCollectorJob : IJob
{
    private readonly MetricAuditStore _store;
    private readonly ILogger<MetricCollectorJob> _logger;
    private static readonly Random Rnd = new();

    public MetricCollectorJob(MetricAuditStore store, ILogger<MetricCollectorJob> logger)
    {
        _store = store;
        _logger = logger;
    }

    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var record = new MetricRecord(DateTime.UtcNow, Rnd.NextDouble() * 100, Rnd.NextDouble() * 4000);
        _store.Add(record);
        _logger.LogInformation("Collected metrics: {@Record}", record);
        return default;
    }
}
