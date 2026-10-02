using ImportStudio.Api.Models;
using ImportStudio.Api.Services;
using Xunit;

namespace ImportStudio.Tests;

public sealed class ImportJobProcessorTests
{
    [Fact]
    public async Task Marks_job_completed_and_counts_valid_and_rejected_rows()
    {
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            FileName = "customers.csv",
            Format = ImportFileFormat.Csv,
            ParseResult = new ImportParseResult
            {
                Format = ImportFileFormat.Csv,
                Headers = ["Name", "Phone"],
                Rows = [new CustomerImportRow("Nguyen An", "0900000000", null, 2)],
                Errors = [new ImportRowError(3, "Phone", "Phone: số điện thoại không được để trống.")],
                TotalRows = 2
            }
        };

        await new ImportJobProcessor().ProcessAsync(job, CancellationToken.None);

        Assert.Equal(ImportJobStatus.Completed, job.Status);
        Assert.Equal(2, job.ProcessedRows);
        Assert.Equal(1, job.ImportedRows);
        Assert.Equal(1, job.RejectedRows);
        Assert.Single(job.ImportedCustomers);
        Assert.NotNull(job.CompletedAt);
    }
}
