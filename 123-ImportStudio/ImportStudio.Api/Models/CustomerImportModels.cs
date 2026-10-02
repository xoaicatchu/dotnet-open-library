namespace ImportStudio.Api.Models;

public enum ImportFileFormat
{
    Csv,
    Xlsx
}

public sealed record CustomerImportRow(
    string FullName,
    string PhoneNumber,
    string? Email,
    int SourceRowNumber);

public sealed record ImportRowError(
    int SourceRowNumber,
    string Field,
    string Message);

public sealed class ImportParseResult
{
    public required ImportFileFormat Format { get; init; }

    public required IReadOnlyList<string> Headers { get; init; }

    public required IReadOnlyList<CustomerImportRow> Rows { get; init; }

    public required IReadOnlyList<ImportRowError> Errors { get; init; }

    public int TotalRows { get; init; }
}
