using Microsoft.Extensions.Hosting;

namespace ImportStudio.Api.Services;

public sealed class ImportJobWorker(
    ImportJobQueue queue,
    ImportJobStore store,
    ImportJobProcessor processor,
    ILogger<ImportJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var jobId in queue.ReadAllAsync(stoppingToken))
        {
            if (!store.TryGet(jobId, out var job) || job is null)
            {
                continue;
            }

            try
            {
                await processor.ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Import job {JobId} failed.", jobId);
            }
        }
    }
}
