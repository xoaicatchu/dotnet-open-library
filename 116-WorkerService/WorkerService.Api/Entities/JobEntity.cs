namespace WorkerService.Api.Entities;

public class JobEntity
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;  // "Email", "Report", "Cleanup"
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public string? Payload { get; set; }  // JSON
    public string? Result { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
