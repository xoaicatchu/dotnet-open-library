using System.Text;
using ImportStudio.Api.Models;
using ImportStudio.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImportStudio.Api.Controllers;

[ApiController]
[Route("api/imports")]
public sealed class ImportsController(
    CustomerImportParser parser,
    ImportJobStore store,
    ImportJobQueue queue) : ControllerBase
{
    [HttpPost("preview")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<ImportPreviewResponse>> Preview(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Hãy chọn một file CSV hoặc Excel (.xlsx)." });
        }

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        ImportParseResult result;
        try
        {
            result = await parser.ParseAsync(stream, file.FileName, cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var job = store.Add(new ImportJob
        {
            Id = Guid.NewGuid(),
            FileName = Path.GetFileName(file.FileName),
            Format = result.Format,
            ParseResult = result
        });

        return CreatedAtAction(
            nameof(GetStatus),
            new { id = job.Id },
            ToPreviewResponse(job));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<ImportStartResponse>> Start(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!store.TryGet(id, out var job) || job is null)
        {
            return NotFound();
        }

        if (job.Status != ImportJobStatus.PreviewReady)
        {
            return Conflict(new { message = "Job này đã được đưa vào hàng đợi hoặc đã hoàn tất." });
        }

        job.Status = ImportJobStatus.Queued;
        await queue.EnqueueAsync(id, cancellationToken);

        return Accepted(new ImportStartResponse(job.Id, job.Status.ToString(), "Job đã được đưa vào hàng đợi xử lý nền."));
    }

    [HttpGet("{id:guid}")]
    public ActionResult<ImportStatusResponse> GetStatus(Guid id)
    {
        if (!store.TryGet(id, out var job) || job is null)
        {
            return NotFound();
        }

        return Ok(ToStatusResponse(job));
    }

    [HttpGet("{id:guid}/errors")]
    public ActionResult<IReadOnlyList<ImportRowError>> GetErrors(Guid id)
    {
        if (!store.TryGet(id, out var job) || job is null)
        {
            return NotFound();
        }

        return Ok(job.ParseResult.Errors);
    }

    [HttpGet("{id:guid}/customers")]
    public ActionResult<IReadOnlyList<CustomerImportRow>> GetImportedCustomers(Guid id)
    {
        if (!store.TryGet(id, out var job) || job is null)
        {
            return NotFound();
        }

        return Ok(job.ImportedCustomers);
    }

    [HttpGet("{id:guid}/errors.csv")]
    public ActionResult DownloadErrors(Guid id)
    {
        if (!store.TryGet(id, out var job) || job is null)
        {
            return NotFound();
        }

        var csv = new StringBuilder()
            .AppendLine("Row,Field,Message");

        foreach (var error in job.ParseResult.Errors)
        {
            csv.Append(error.SourceRowNumber)
                .Append(',')
                .Append(EscapeCsv(error.Field))
                .Append(',')
                .Append(EscapeCsv(error.Message))
                .AppendLine();
        }

        return File(
            Encoding.UTF8.GetBytes(csv.ToString()),
            "text/csv; charset=utf-8",
            $"{Path.GetFileNameWithoutExtension(job.FileName)}-errors.csv");
    }

    private static ImportPreviewResponse ToPreviewResponse(ImportJob job)
    {
        return new ImportPreviewResponse(
            job.Id,
            job.FileName,
            job.Format.ToString(),
            job.ParseResult.Headers,
            job.ParseResult.TotalRows,
            job.ParseResult.Rows.Count,
            job.ParseResult.Errors.Select(error => error.SourceRowNumber).Distinct().Count(),
            job.ParseResult.Errors.Take(20).ToArray());
    }

    private static ImportStatusResponse ToStatusResponse(ImportJob job)
    {
        return new ImportStatusResponse(
            job.Id,
            job.FileName,
            job.Status.ToString(),
            job.ParseResult.TotalRows,
            job.ProcessedRows,
            job.ImportedRows,
            job.RejectedRows,
            job.FailureReason,
            job.CompletedAt);
    }

    private static string EscapeCsv(string value)
    {
        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }
}

public sealed record ImportPreviewResponse(
    Guid Id,
    string FileName,
    string Format,
    IReadOnlyList<string> Headers,
    int TotalRows,
    int ValidRows,
    int ErrorRows,
    IReadOnlyList<ImportRowError> Errors);

public sealed record ImportStartResponse(Guid Id, string Status, string Message);

public sealed record ImportStatusResponse(
    Guid Id,
    string FileName,
    string Status,
    int TotalRows,
    int ProcessedRows,
    int ImportedRows,
    int RejectedRows,
    string? FailureReason,
    DateTimeOffset? CompletedAt);
