using Coravel.Invocable;
using TaskQueueScheduler.Api.Data;
using TaskQueueScheduler.Api.Models;

namespace TaskQueueScheduler.Api.Invocables;

public class DataProcessingInvocable : IInvocable, IInvocableWithPayload<TaskPayload>
{
    private readonly TaskAuditStore _store;
    private readonly ILogger<DataProcessingInvocable> _logger;

    public TaskPayload Payload { get; set; } = default!;

    public DataProcessingInvocable(TaskAuditStore store, ILogger<DataProcessingInvocable> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("Processing {RecordCount} records for {Title}...", Payload.RecordCount, Payload.Title);
        
        // Simulate work
        await Task.Delay(100);
        
        _store.RecordTask(
            Guid.NewGuid().ToString(), 
            "DataProcessing", 
            $"Processed {Payload.RecordCount} records for '{Payload.Title}'", 
            DateTime.UtcNow);
            
        _logger.LogInformation("Data processing complete.");
    }
}
