namespace ImportStudio.Api.Models;

public enum ImportJobStatus
{
    PreviewReady,
    Queued,
    Running,
    Completed,
    Failed
}

public sealed class ImportJob
{
    public required Guid Id { get; init; }

    public required string FileName { get; init; }

    public required ImportFileFormat Format { get; init; }

    public required ImportParseResult ParseResult { get; init; }

    public ImportJobStatus Status { get; set; } = ImportJobStatus.PreviewReady;

    public int ProcessedRows { get; set; }

    public int ImportedRows { get; set; }

    public int RejectedRows { get; set; }

    public List<CustomerImportRow> ImportedCustomers { get; } = [];

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }

    public string? FailureReason { get; set; }
}
