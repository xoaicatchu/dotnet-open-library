using ImportStudio.Api.Models;

namespace ImportStudio.Api.Services;

public sealed class ImportJobProcessor
{
    public async Task ProcessAsync(ImportJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        job.Status = ImportJobStatus.Running;
        job.RejectedRows = Math.Max(0, job.ParseResult.TotalRows - job.ParseResult.Rows.Count);

        try
        {
            foreach (var row in job.ParseResult.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                job.ImportedCustomers.Add(row);
                job.ImportedRows++;
                await Task.Yield();
            }

            job.ProcessedRows = job.ParseResult.TotalRows;
            job.Status = ImportJobStatus.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            job.Status = ImportJobStatus.Failed;
            job.FailureReason = exception.Message;
            job.CompletedAt = DateTimeOffset.UtcNow;
            throw;
        }
    }
}
