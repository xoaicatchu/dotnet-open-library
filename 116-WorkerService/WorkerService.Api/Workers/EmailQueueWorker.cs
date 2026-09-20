using WorkerService.Api.Data;
using WorkerService.Api.Entities;
using WorkerService.Api.Services;

namespace WorkerService.Api.Workers;

public class EmailQueueWorker : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly ILogger<EmailQueueWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public EmailQueueWorker(IEmailQueue queue, ILogger<EmailQueueWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _queue = queue;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailQueueWorker started");
        
        await foreach (var email in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                // Simulate email send
                await Task.Delay(100, stoppingToken); // simulate work
                _logger.LogInformation("Email sent to {To}: {Subject}", email.To, email.Subject);
                
                // Record to DB
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Jobs.Add(new JobEntity {
                    Type = "Email",
                    Status = JobStatus.Completed,
                    Payload = $"{{\"to\":\"{email.To}\"}}",
                    Result = "Sent",
                    StartedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", email.To);
            }
        }
        _logger.LogInformation("EmailQueueWorker stopped");
    }
}
