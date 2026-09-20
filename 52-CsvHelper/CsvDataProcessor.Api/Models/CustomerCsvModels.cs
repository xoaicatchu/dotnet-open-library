using CsvHelper.Configuration;

namespace CsvDataProcessor.Api.Models;

public class CustomerRecord
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public decimal Balance { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegisteredDate { get; set; }
}

public sealed class CustomerRecordMap : ClassMap<CustomerRecord>
{
    public CustomerRecordMap()
    {
        Map(m => m.Id).Name("Customer ID");
        Map(m => m.FullName).Name("Full Name");
        Map(m => m.Email).Name("Email Address");
        Map(m => m.PhoneNumber).Name("Phone Number");
        Map(m => m.Balance).Name("Account Balance").TypeConverterOption.Format("F2");
        Map(m => m.IsActive).Name("Active Status");
        Map(m => m.RegisteredDate).Name("Registration Date").TypeConverterOption.Format("yyyy-MM-dd");
    }
}

public record CsvImportResultDto(
    int TotalProcessed,
    int SuccessCount,
    int ErrorCount,
    List<CustomerRecord> Records,
    List<string> Errors);
