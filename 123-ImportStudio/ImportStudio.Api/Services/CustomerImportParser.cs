using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ImportStudio.Api.Models;

namespace ImportStudio.Api.Services;

public sealed class CustomerImportParser
{
    private static readonly Regex NonAlphaNumeric = new("[^a-z0-9]", RegexOptions.Compiled);

    private static readonly string[] FullNameAliases = ["fullname", "customername", "name", "hoten"];

    private static readonly string[] PhoneAliases = ["phone", "phonenumber", "mobile", "sodienthoai"];

    private static readonly string[] EmailAliases = ["email", "emailaddress", "mail"];

    public async Task<ImportParseResult> ParseAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".csv" => await ParseCsvAsync(stream, cancellationToken),
            ".xlsx" => ParseXlsx(stream, cancellationToken),
            _ => throw new ArgumentException("Chỉ hỗ trợ tệp .csv hoặc .xlsx.", nameof(fileName))
        };
    }

    private static async Task<ImportParseResult> ParseCsvAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, configuration);
        if (!await csv.ReadAsync())
        {
            return EmptyResult(ImportFileFormat.Csv);
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? [];
        var indexes = CreateHeaderIndexes(headers);
        var rows = new List<CustomerImportRow>();
        var errors = new List<ImportRowError>();
        var totalRows = 0;

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalRows++;
            var sourceRow = csv.Parser.Row;
            var values = headers.ToDictionary(header => header, header => csv.GetField(header) ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            ValidateAndAdd(values, indexes, sourceRow, rows, errors);
        }

        return new ImportParseResult
        {
            Format = ImportFileFormat.Csv,
            Headers = headers,
            Rows = rows,
            Errors = errors,
            TotalRows = totalRows
        };
    }

    private static ImportParseResult ParseXlsx(Stream stream, CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet is null)
        {
            return EmptyResult(ImportFileFormat.Xlsx);
        }

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return EmptyResult(ImportFileFormat.Xlsx);
        }

        var firstRow = usedRange.FirstRowUsed();
        var lastRow = usedRange.LastRowUsed();
        var headers = firstRow.CellsUsed().Select(cell => cell.GetString()).ToArray();
        var indexes = CreateHeaderIndexes(headers);
        var rows = new List<CustomerImportRow>();
        var errors = new List<ImportRowError>();
        var totalRows = 0;

        foreach (var row in worksheet.Rows(firstRow.RowNumber() + 1, lastRow.RowNumber()))
        {
            cancellationToken.ThrowIfCancellationRequested();
            totalRows++;
            var values = headers
                .Select((header, index) => new { header, Value = row.Cell(index + 1).GetString() })
                .ToDictionary(item => item.header, item => item.Value, StringComparer.OrdinalIgnoreCase);
            ValidateAndAdd(values, indexes, row.RowNumber(), rows, errors);
        }

        return new ImportParseResult
        {
            Format = ImportFileFormat.Xlsx,
            Headers = headers,
            Rows = rows,
            Errors = errors,
            TotalRows = totalRows
        };
    }

    private static Dictionary<string, string> CreateHeaderIndexes(IReadOnlyList<string> headers)
    {
        return headers
            .Where(header => !string.IsNullOrWhiteSpace(header))
            .ToDictionary(Normalize, header => header, StringComparer.OrdinalIgnoreCase);
    }

    private static void ValidateAndAdd(
        IReadOnlyDictionary<string, string> values,
        IReadOnlyDictionary<string, string> indexes,
        int sourceRow,
        ICollection<CustomerImportRow> rows,
        ICollection<ImportRowError> errors)
    {
        var fullName = ReadValue(values, indexes, FullNameAliases);
        var phone = ReadValue(values, indexes, PhoneAliases);
        var email = ReadValue(values, indexes, EmailAliases);

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add(new ImportRowError(sourceRow, "FullName", "Họ tên không được để trống."));
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            errors.Add(new ImportRowError(sourceRow, "Phone", "Phone: số điện thoại không được để trống."));
        }
        else if (phone.Any(character => !char.IsDigit(character) && !" +()-".Contains(character)))
        {
            errors.Add(new ImportRowError(sourceRow, "Phone", "Phone: số điện thoại chứa ký tự không hợp lệ."));
        }

        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@', StringComparison.Ordinal))
        {
            errors.Add(new ImportRowError(sourceRow, "Email", "Email phải chứa ký tự @."));
        }

        if (errors.Any(error => error.SourceRowNumber == sourceRow))
        {
            return;
        }

        rows.Add(new CustomerImportRow(fullName!, phone!, email, sourceRow));
    }

    private static string? ReadValue(
        IReadOnlyDictionary<string, string> values,
        IReadOnlyDictionary<string, string> indexes,
        IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
        {
            if (indexes.TryGetValue(alias, out var header) && values.TryGetValue(header, out var value))
            {
                return value?.Trim();
            }
        }

        return null;
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var withoutDiacritics = new string(
            decomposed
                .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                .ToArray());

        return NonAlphaNumeric.Replace(withoutDiacritics.ToLowerInvariant(), string.Empty);
    }

    private static ImportParseResult EmptyResult(ImportFileFormat format)
    {
        return new ImportParseResult
        {
            Format = format,
            Headers = [],
            Rows = [],
            Errors = [],
            TotalRows = 0
        };
    }
}
