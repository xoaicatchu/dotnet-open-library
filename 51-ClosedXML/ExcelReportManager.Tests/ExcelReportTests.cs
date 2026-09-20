using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ExcelReportManager.Api.Models;
using Xunit;

namespace ExcelReportManager.Tests;

public class ExcelReportTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExcelReportTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSampleData_ReturnsExpectedRecords()
    {
        var response = await _client.GetAsync("/api/reports/sample-data");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<List<SaleRecordDto>>();
        Assert.NotNull(data);
        Assert.Equal(4, data.Count);
        Assert.Equal("TXN-1001", data[0].TransactionId);
    }

    [Fact]
    public async Task Export_ReturnsValidXlsxStream()
    {
        // 1. Get sample data
        var sampleResponse = await _client.GetAsync("/api/reports/sample-data");
        var records = await sampleResponse.Content.ReadFromJsonAsync<List<SaleRecordDto>>();
        Assert.NotNull(records);

        // 2. Export to Excel
        var exportResponse = await _client.PostAsJsonAsync("/api/reports/export", records);
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", exportResponse.Content.Headers.ContentType?.MediaType);

        var bytes = await exportResponse.Content.ReadAsByteArrayAsync();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 1000);

        // Zip file signature check (PK\x03\x04) since .xlsx is a ZIP package
        Assert.Equal(0x50, bytes[0]); // 'P'
        Assert.Equal(0x4B, bytes[1]); // 'K'
    }

    [Fact]
    public async Task Export_EmptyList_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/reports/export", new List<SaleRecordDto>());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RoundTrip_ExportThenImport_PreservesData()
    {
        // 1. Get sample data
        var sampleResponse = await _client.GetAsync("/api/reports/sample-data");
        var originalRecords = await sampleResponse.Content.ReadFromJsonAsync<List<SaleRecordDto>>();
        Assert.NotNull(originalRecords);

        // 2. Export to Excel
        var exportResponse = await _client.PostAsJsonAsync("/api/reports/export", originalRecords);
        var xlsxBytes = await exportResponse.Content.ReadAsByteArrayAsync();

        // 3. Import the generated Excel file via multipart form-data
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(xlsxBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "test_report.xlsx");

        var importResponse = await _client.PostAsync("/api/reports/import", content);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var result = await importResponse.Content.ReadFromJsonAsync<ExcelImportResultDto>();
        Assert.NotNull(result);
        Assert.Equal(originalRecords.Count, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(originalRecords[0].TransactionId, result.Records[0].TransactionId);
        Assert.Equal(originalRecords[0].CustomerName, result.Records[0].CustomerName);
        Assert.Equal(originalRecords[0].Quantity, result.Records[0].Quantity);
        Assert.Equal(originalRecords[0].UnitPrice, result.Records[0].UnitPrice);
    }

    [Fact]
    public async Task Import_InvalidFileExtension_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StringContent("fake,csv,content");
        content.Add(fileContent, "file", "test.csv");

        var response = await _client.PostAsync("/api/reports/import", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsAccessible()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
