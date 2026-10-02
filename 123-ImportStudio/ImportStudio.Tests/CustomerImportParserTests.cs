using ClosedXML.Excel;
using ImportStudio.Api.Models;
using ImportStudio.Api.Services;
using Xunit;

namespace ImportStudio.Tests;

public sealed class CustomerImportParserTests
{
    private readonly CustomerImportParser _parser = new();

    [Fact]
    public async Task Parses_csv_and_maps_customer_headers()
    {
        await using var stream = new MemoryStream(
            "Customer Name,Phone,Email\nNguyen An,0900000000,an@example.com\n"u8.ToArray());

        var result = await _parser.ParseAsync(stream, "customers.csv");

        Assert.Equal(ImportFileFormat.Csv, result.Format);
        Assert.Equal(["Customer Name", "Phone", "Email"], result.Headers);
        Assert.Single(result.Rows);
        Assert.Equal("Nguyen An", result.Rows[0].FullName);
        Assert.Equal("0900000000", result.Rows[0].PhoneNumber);
    }

    [Fact]
    public async Task Parses_xlsx_and_maps_customer_headers()
    {
        await using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Customers");
            sheet.Cell(1, 1).Value = "Full Name";
            sheet.Cell(1, 2).Value = "Phone Number";
            sheet.Cell(2, 1).Value = "Tran Binh";
            sheet.Cell(2, 2).Value = "0911111111";
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var result = await _parser.ParseAsync(stream, "customers.xlsx");

        Assert.Equal(ImportFileFormat.Xlsx, result.Format);
        Assert.Single(result.Rows);
        Assert.Equal("Tran Binh", result.Rows[0].FullName);
        Assert.Equal("0911111111", result.Rows[0].PhoneNumber);
    }

    [Fact]
    public async Task Reports_row_errors_without_discarding_valid_rows()
    {
        await using var stream = new MemoryStream(
                "Name,Phone,Email\nValid User,0900000000,valid@example.com\nMissing Phone,,valid@example.com\n"u8.ToArray());

        var result = await _parser.ParseAsync(stream, "customers.csv");

        Assert.Single(result.Rows);
        Assert.Equal(2, result.TotalRows);
        Assert.Single(result.Errors);
        Assert.Contains("Phone", result.Errors[0].Message);
    }
}
