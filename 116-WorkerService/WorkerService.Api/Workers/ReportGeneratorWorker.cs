using WorkerService.Api.Data;
using WorkerService.Api.Entities;

namespace WorkerService.Api.Workers;

public class ReportGeneratorWorker : BackgroundService
{
    private readonly ILogger<ReportGeneratorWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    
    public ReportGeneratorWorker(ILogger<ReportGeneratorWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReportGeneratorWorker started, interval: {Interval}s", _interval.TotalSeconds);
        
        using var timer = new PeriodicTimer(_interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await GenerateReportAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
            }
        }
    }
    
    private async Task GenerateReportAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = new JobEntity {
            Type = "Report",
            Status = JobStatus.Completed,
            Result = $"Report generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Report generated: Job #{JobId}", job.Id);
    }
}
