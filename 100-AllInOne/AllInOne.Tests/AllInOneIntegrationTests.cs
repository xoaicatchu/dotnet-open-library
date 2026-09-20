using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using AllInOne.Api.Models;
using Xunit;

namespace AllInOne.Tests;

public class AllInOneIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AllInOneIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Test_01_GetOrders_ReturnsDashboard_Dapper()
    {
        var response = await _client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<OrderSummaryDto>>();
        orders.Should().NotBeNull();
        orders.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Test_02_GetOrderById_ReturnsOrder_MediatR_EFCore()
    {
        var response = await _client.GetAsync("/api/orders/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();
        order!.Id.Should().Be(1);
        order.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Test_03_CreateOrder_ValidInput_CreatesAndTransitions_Stateless_MassTransit_CAP()
    {
        var request = new CreateOrderRequest(
            "Alice Smith",
            "alice.smith@example.com",
            new List<CreateOrderItemRequest>
            {
                new("Cloud Server Node", 2, 250.00m),
                new("Domain Registration", 1, 15.00m)
            });

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<OrderDto>();
        created.Should().NotBeNull();
        created!.CustomerName.Should().Be("Alice Smith");
        created.TotalAmount.Should().Be(515.00m);
        created.Status.Should().Be("Submitted"); // Transitioned via Stateless
        created.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Test_04_CreateOrder_InvalidInput_ReturnsBadRequest_FluentValidation()
    {
        var invalid = new CreateOrderRequest("", "invalid-email", new List<CreateOrderItemRequest>());
        var response = await _client.PostAsJsonAsync("/api/orders", invalid);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Test_05_TransitionOrder_InvalidTrigger_ReturnsBadRequest_Stateless()
    {
        // Try to directly Complete a Submitted order without Approving/Shipping first
        var response = await _client.PostAsync("/api/orders/1/transition?trigger=Complete", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Test_06_RunApprovalWorkflow_Under1000_AutoApproved_Elsa()
    {
        var createRequest = new CreateOrderRequest(
            "Small Order Customer",
            "small@test.com",
            new List<CreateOrderItemRequest> { new("USB Flash Drive", 1, 45.00m) });

        var createResponse = await _client.PostAsJsonAsync("/api/orders", createRequest);
        var order = await createResponse.Content.ReadFromJsonAsync<OrderDto>();
        order.Should().NotBeNull();

        // Run Elsa workflow
        var workflowResponse = await _client.PostAsync($"/api/orders/{order!.Id}/approval-workflow", null);
        workflowResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await workflowResponse.Content.ReadFromJsonAsync<WorkflowResultDto>();
        result.Should().NotBeNull();
        result!.IsApproved.Should().BeTrue();
        result.CurrentStatus.Should().Be("Approved");
    }

    [Fact]
    public async Task Test_07_DownloadInvoicePdf_ReturnsValidPdfBytes_QuestPDF()
    {
        var response = await _client.GetAsync("/api/orders/1/invoice-pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        // PDF signature '%PDF-'
        var header = Encoding.ASCII.GetString(bytes, 0, 5);
        header.Should().Be("%PDF-");
    }

    [Fact]
    public async Task Test_08_ExportExcel_ReturnsValidXlsxPackage_ClosedXML()
    {
        var response = await _client.GetAsync("/api/orders/export-excel");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(1000);

        // ZIP signature (PK\x03\x04)
        bytes[0].Should().Be(0x50); // 'P'
        bytes[1].Should().Be(0x4B); // 'K'
    }

    [Fact]
    public async Task Test_09_ExportAndImportCsv_PreservesData_CsvHelper()
    {
        // 1. Export CSV
        var exportResponse = await _client.GetAsync("/api/orders/export-csv");
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var csvBytes = await exportResponse.Content.ReadAsByteArrayAsync();

        // 2. Import CSV via form-data
        using var content = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(csvBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/csv");
        content.Add(fileContent, "file", "test_orders.csv");

        var importResponse = await _client.PostAsync("/api/orders/import-csv", content);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Test_10_Swagger_IsAccessible()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record WorkflowResultDto(int OrderId, string OrderNumber, decimal Amount, bool IsApproved, string Reason, string CurrentStatus);
}
