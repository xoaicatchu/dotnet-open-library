using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvDataProcessor.Api.Models;

namespace CsvDataProcessor.Api.Services;

public class CsvService
{
    private readonly CsvConfiguration _csvConfig;

    public CsvService()
    {
        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null
        };
    }

    public byte[] ExportCustomersCsv(List<CustomerRecord> customers)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, _csvConfig);

        csv.Context.RegisterClassMap<CustomerRecordMap>();
        csv.WriteRecords(customers);
        writer.Flush();

        return memoryStream.ToArray();
    }

    public async Task<CsvImportResultDto> ImportCustomersCsvAsync(Stream stream)
    {
        var records = new List<CustomerRecord>();
        var errors = new List<string>();
        int totalProcessed = 0;

        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var csv = new CsvReader(reader, _csvConfig);

        csv.Context.RegisterClassMap<CustomerRecordMap>();

        try
        {
            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                totalProcessed++;
                try
                {
                    var record = csv.GetRecord<CustomerRecord>();
                    if (record != null)
                    {
                        if (string.IsNullOrWhiteSpace(record.Id))
                        {
                            errors.Add($"Row {totalProcessed + 1}: Customer ID cannot be empty");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(record.Email) || !record.Email.Contains('@'))
                        {
                            errors.Add($"Row {totalProcessed + 1}: Invalid email address '{record.Email}'");
                            continue;
                        }

                        records.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {totalProcessed + 1}: Parse error - {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Fatal CSV read error: {ex.Message}");
        }

        return new CsvImportResultDto(
            totalProcessed,
            records.Count,
            errors.Count,
            records,
            errors);
    }
}
