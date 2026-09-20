namespace WorkerService.Api.Workers;

public class JobStatusMonitor : IHostedService
{
    private readonly ILogger<JobStatusMonitor> _logger;

    public JobStatusMonitor(ILogger<JobStatusMonitor> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JobStatusMonitor started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("JobStatusMonitor stopped");
        return Task.CompletedTask;
    }
}
