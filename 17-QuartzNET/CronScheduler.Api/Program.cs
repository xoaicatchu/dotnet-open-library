using CronScheduler.Api.Data;
using CronScheduler.Api.Jobs;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<MetricAuditStore>();
builder.Services.AddSingleton<BackupAuditStore>();

builder.Services.AddQuartz(q =>
{
    // Auto-scheduled background job with 2-second interval for easy test verification
    var metricJobKey = new JobKey("metricCollector", "maintenance");
    q.AddJob<MetricCollectorJob>(opts => opts.WithIdentity(metricJobKey));
    q.AddTrigger(opts => opts
        .ForJob(metricJobKey)
        .WithIdentity("metricCollector-trigger", "maintenance")
        .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromSeconds(2)).RepeatForever()));

    // Define the backup job (can be triggered on-demand with JobDataMap)
    var backupJobKey = new JobKey("databaseBackup", "maintenance");
    q.AddJob<DatabaseBackupJob>(opts => opts.WithIdentity(backupJobKey).StoreDurably());
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program { }
