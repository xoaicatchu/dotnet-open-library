using StackExchange.Redis;

namespace AspireDemo.Worker;

/// <summary>
/// Background service: poll Redis queue, xử lý task async.
/// Aspire tự kết nối Redis và inject IConnectionMultiplexer.
/// Traces xuất sang Aspire Dashboard để theo dõi real-time.
/// </summary>
public sealed class TaskProcessorWorker : BackgroundService
{
    private const string QueueKey = "tasks:pending";
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<TaskProcessorWorker> _logger;

    public TaskProcessorWorker(IConnectionMultiplexer redis, ILogger<TaskProcessorWorker> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TaskProcessorWorker started — polling Redis queue '{Queue}'", QueueKey);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var db = _redis.GetDatabase();
                var value = await db.ListRightPopAsync(QueueKey);

                if (value.IsNullOrEmpty)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                if (!int.TryParse(value, out var taskId))
                {
                    _logger.LogWarning("Invalid task ID from queue: {Value}", value);
                    continue;
                }

                await ProcessTaskAsync(taskId, db, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task from queue");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("TaskProcessorWorker stopped");
    }

    private async Task ProcessTaskAsync(int taskId, IDatabase db, CancellationToken ct)
    {
        _logger.LogInformation("Processing task {TaskId}...", taskId);
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Simulate business logic (send email, generate report, call external API…)
            await Task.Delay(Random.Shared.Next(200, 800), ct);

            var resultKey = $"task:{taskId}:result";
            await db.HashSetAsync(resultKey, [
                new HashEntry("status", "Completed"),
                new HashEntry("processedAt", DateTime.UtcNow.ToString("O")),
                new HashEntry("processingMs", sw.ElapsedMilliseconds)
            ]);
            await db.KeyExpireAsync(resultKey, TimeSpan.FromHours(1));

            sw.Stop();
            _logger.LogInformation("Task {TaskId} completed in {Ms}ms", taskId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Task {TaskId} failed", taskId);
            var resultKey = $"task:{taskId}:result";
            await db.HashSetAsync(resultKey, [
                new HashEntry("status", "Failed"),
                new HashEntry("error", ex.Message)
            ]);
        }
    }
}
