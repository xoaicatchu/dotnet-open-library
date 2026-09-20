namespace TaskQueueScheduler.Api.Models;

public record TaskPayload(string Title, int RecordCount);

public record QueueTaskRequest(string Title, int RecordCount);
