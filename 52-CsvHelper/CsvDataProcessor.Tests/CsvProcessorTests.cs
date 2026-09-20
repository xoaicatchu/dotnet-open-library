using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using CsvDataProcessor.Api.Models;
using Xunit;

namespace CsvDataProcessor.Tests;

public class CsvProcessorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CsvProcessorTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSample_ReturnsExpectedCustomers()
    {
        var response = await _client.GetAsync("/api/csv/sample");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var customers = await response.Content.ReadFromJsonAsync<List<CustomerRecord>>();
        Assert.NotNull(customers);
        Assert.Equal(3, customers.Count);
        Assert.Equal("CUST-001", customers[0].Id);
    }

    [Fact]
    public async Task Export_ReturnsValidCsvFileWithMappedHeaders()
    {
        var sampleResponse = await _client.GetAsync("/api/csv/sample");
        var customers = await sampleResponse.Content.ReadFromJsonAsync<List<CustomerRecord>>();
        Assert.NotNull(customers);

        var exportResponse = await _client.PostAsJsonAsync("/api/csv/export", customers);
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("text/csv", exportResponse.Content.Headers.ContentType?.MediaType);

        var csvString = await exportResponse.Content.ReadAsStringAsync();
        Assert.NotNull(csvString);

        // Check custom ClassMap headers
        Assert.Contains("Customer ID,Full Name,Email Address,Phone Number,Account Balance,Active Status,Registration Date", csvString);
        Assert.Contains("CUST-001,Nguyen Van A,anguyen@example.com", csvString);
    }

    [Fact]
    public async Task Export_EmptyList_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/csv/export", new List<CustomerRecord>());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RoundTrip_ExportThenImport_PreservesData()
    {
        // 1. Get sample data
        var sampleResponse = await _client.GetAsync("/api/csv/sample");
        var original = await sampleResponse.Content.ReadFromJsonAsync<List<CustomerRecord>>();
        Assert.NotNull(original);

        // 2. Export to CSV bytes
        var exportResponse = await _client.PostAsJsonAsync("/api/csv/export", original);
        var csvBytes = await exportResponse.Content.ReadAsByteArrayAsync();

        // 3. Import via multipart form-data
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(csvBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "customers_test.csv");

        var importResponse = await _client.PostAsync("/api/csv/import", content);
        Assert.Equal(HttpStatusCode.OK, importResponse.StatusCode);

        var result = await importResponse.Content.ReadFromJsonAsync<CsvImportResultDto>();
        Assert.NotNull(result);
        Assert.Equal(original.Count, result.SuccessCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.Equal(original[0].Id, result.Records[0].Id);
        Assert.Equal(original[0].FullName, result.Records[0].FullName);
        Assert.Equal(original[0].Email, result.Records[0].Email);
        Assert.Equal(original[0].Balance, result.Records[0].Balance);
    }

    [Fact]
    public async Task Import_InvalidFileExtension_ReturnsBadRequest()
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StringContent("some,data");
        content.Add(fileContent, "file", "data.json");

        var response = await _client.PostAsync("/api/csv/import", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_InvalidRecords_CapturesErrors()
    {
        var csvContent = new StringBuilder();
        csvContent.AppendLine("Customer ID,Full Name,Email Address,Phone Number,Account Balance,Active Status,Registration Date");
        csvContent.AppendLine("CUST-101,Valid User,valid@test.com,+123456,100.00,True,2026-01-01");
        csvContent.AppendLine(",Empty ID User,empty@test.com,+123456,200.00,True,2026-01-01"); // Empty ID
        csvContent.AppendLine("CUST-103,Bad Email,invalidemailformat,+123456,300.00,True,2026-01-01"); // Bad Email

        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csvContent.ToString()));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "mixed.csv");

        var response = await _client.PostAsync("/api/csv/import", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CsvImportResultDto>();
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalProcessed);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(2, result.ErrorCount);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Swagger_IsAccessible()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
