using CronScheduler.Api.Data;
using CronScheduler.Api.Models;
using Quartz;

namespace CronScheduler.Api.Jobs;

public class DatabaseBackupJob : IJob
{
    private readonly BackupAuditStore _store;
    private readonly ILogger<DatabaseBackupJob> _logger;

    public DatabaseBackupJob(BackupAuditStore store, ILogger<DatabaseBackupJob> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var backupType = context.MergedJobDataMap.GetString("backupType") ?? "Incremental";
        _logger.LogInformation("Starting backup of type {BackupType}...", backupType);
        
        var startedAt = DateTime.UtcNow;
        var backupId = Guid.NewGuid().ToString();

        // Simulate backup
        await Task.Delay(500);

        var record = new BackupRecord(backupId, backupType, startedAt, DateTime.UtcNow, "Success");
        _store.Add(record);
        _logger.LogInformation("Completed backup {@Record}", record);
    }
}
