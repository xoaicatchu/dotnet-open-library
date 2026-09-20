namespace CronScheduler.Api.Data;
using CronScheduler.Api.Models;

public class BackupAuditStore
{
    private readonly List<BackupRecord> _backups = new();
    private readonly object _lock = new();

    public void Add(BackupRecord record)
    {
        lock (_lock)
        {
            _backups.Add(record);
        }
    }

    public IReadOnlyList<BackupRecord> GetAll()
    {
        lock (_lock)
        {
            return _backups.ToList();
        }
    }
}
