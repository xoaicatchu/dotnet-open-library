namespace CronScheduler.Api.Models;

public record MetricRecord(DateTime Timestamp, double CpuPercentage, double MemoryUsedMb);
public record BackupRecord(string BackupId, string BackupType, DateTime StartedAt, DateTime CompletedAt, string Status);
public record BackupRequest(string BackupType);
