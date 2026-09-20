using CronScheduler.Api.Data;
using CronScheduler.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Quartz;

namespace CronScheduler.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchedulerController : ControllerBase
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly MetricAuditStore _metricStore;
    private readonly BackupAuditStore _backupStore;

    public SchedulerController(
        ISchedulerFactory schedulerFactory,
        MetricAuditStore metricStore,
        BackupAuditStore backupStore)
    {
        _schedulerFactory = schedulerFactory;
        _metricStore = metricStore;
        _backupStore = backupStore;
    }

    [HttpPost("backup/trigger")]
    public async Task<IActionResult> TriggerBackup([FromBody] BackupRequest req)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var data = new JobDataMap { { "backupType", req.BackupType } };
        await scheduler.TriggerJob(new JobKey("databaseBackup", "maintenance"), data);
        return Accepted("/api/scheduler/backups", new { Message = "Backup triggered", req.BackupType });
    }

    [HttpPost("jobs/{jobName}/pause")]
    public async Task<IActionResult> PauseJob(string jobName)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.PauseJob(new JobKey(jobName, "maintenance"));
        return Ok(new { Message = $"Job {jobName} paused" });
    }

    [HttpPost("jobs/{jobName}/resume")]
    public async Task<IActionResult> ResumeJob(string jobName)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.ResumeJob(new JobKey(jobName, "maintenance"));
        return Ok(new { Message = $"Job {jobName} resumed" });
    }

    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        return Ok(_metricStore.GetAll());
    }

    [HttpGet("backups")]
    public IActionResult GetBackups()
    {
        return Ok(_backupStore.GetAll());
    }
}
