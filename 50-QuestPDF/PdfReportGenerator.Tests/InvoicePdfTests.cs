using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PdfReportGenerator.Api.Models;
using Xunit;

namespace PdfReportGenerator.Tests;

public class InvoicePdfTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InvoicePdfTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSample_ReturnsValidSampleInvoice()
    {
        var response = await _client.GetAsync("/api/invoices/sample");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sample = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(sample);
        Assert.Equal("INV-2026-001", sample.InvoiceNumber);
        Assert.NotEmpty(sample.Items);
        Assert.True(sample.GrandTotal > 0);
    }

    [Fact]
    public async Task GeneratePdf_ReturnsValidPdfBytesAndHeaders()
    {
        // 1. Get sample
        var sampleResponse = await _client.GetAsync("/api/invoices/sample");
        var invoice = await sampleResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(invoice);

        // 2. Generate PDF
        var pdfResponse = await _client.PostAsJsonAsync("/api/invoices/generate-pdf", invoice);
        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);

        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000); // Standard PDF header + layout is usually > 1KB

        // Check PDF header signature '%PDF-'
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task GetMetadata_ReturnsCorrectPdfInfo()
    {
        var sampleResponse = await _client.GetAsync("/api/invoices/sample");
        var invoice = await sampleResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(invoice);

        var metaResponse = await _client.PostAsJsonAsync("/api/invoices/metadata", invoice);
        Assert.Equal(HttpStatusCode.OK, metaResponse.StatusCode);

        var metadata = await metaResponse.Content.ReadFromJsonAsync<PdfMetadataDto>();
        Assert.NotNull(metadata);
        Assert.Equal("INV-2026-001", metadata.InvoiceNumber);
        Assert.Equal("application/pdf", metadata.ContentType);
        Assert.True(metadata.FileSizeBytes > 0);
    }

    [Fact]
    public async Task GeneratePdf_EmptyInvoiceNumber_ReturnsBadRequest()
    {
        var sampleResponse = await _client.GetAsync("/api/invoices/sample");
        var invoice = await sampleResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(invoice);

        var invalid = invoice with { InvoiceNumber = "" };
        var response = await _client.PostAsJsonAsync("/api/invoices/generate-pdf", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GeneratePdf_EmptyItems_ReturnsBadRequest()
    {
        var sampleResponse = await _client.GetAsync("/api/invoices/sample");
        var invoice = await sampleResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(invoice);

        var invalid = invoice with { Items = new List<InvoiceItemDto>() };
        var response = await _client.PostAsJsonAsync("/api/invoices/generate-pdf", invalid);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsAccessible()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
