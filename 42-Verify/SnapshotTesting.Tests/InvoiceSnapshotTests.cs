using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SnapshotTesting.Api.Models;
using Xunit;

namespace SnapshotTesting.Tests;

public class InvoiceSnapshotTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InvoiceSnapshotTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetInvoiceById_SnapshotMatches()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices/11111111-1111-1111-1111-111111111111");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceDetail>();
        Assert.NotNull(invoice);

        await Verify(invoice);
    }

    [Fact]
    public async Task GetInvoiceSummary_SnapshotMatches()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices/11111111-1111-1111-1111-111111111111/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<InvoiceSummary>();
        Assert.NotNull(summary);

        await Verify(summary);
    }

    [Fact]
    public async Task GetAllInvoices_Returns200WithList()
    {
        // Act
        var response = await _client.GetAsync("/api/invoices");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invoices = await response.Content.ReadFromJsonAsync<List<InvoiceSummary>>();
        Assert.NotNull(invoices);
        Assert.NotEmpty(invoices);
    }

    [Fact]
    public async Task GetInvoiceById_NotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/invoices/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvoice_ValidRequest_Returns201WithCalculatedValues()
    {
        // Arrange
        var request = new CreateInvoiceRequest(
            Customer: new CustomerInfo(
                Id: Guid.NewGuid(),
                Name: "Tech Solutions Ltd",
                TaxCode: "0109988776",
                Email: "billing@techsolutions.com",
                Address: "789 Coding Blvd, Da Nang"
            ),
            Items:
            [
                new CreateInvoiceItemRequest(
                    Description: "Custom API Development",
                    UnitPrice: 2000.00m,
                    Quantity: 2,
                    Discount: 500.00m
                )
            ],
            TaxRate: 0.10m,
            Notes: "Urgent project delivery"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoices", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<InvoiceDetail>();
        Assert.NotNull(created);
        Assert.Equal(4000.00m, created.SubTotal);
        Assert.Equal(500.00m, created.DiscountTotal);
        Assert.Equal(350.00m, created.TaxAmount);
        Assert.Equal(3850.00m, created.GrandTotal);
    }

    [Fact]
    public async Task CreateInvoice_MissingCustomer_Returns400()
    {
        // Arrange
        var request = new CreateInvoiceRequest(
            Customer: new CustomerInfo(Guid.NewGuid(), "", "", "", ""),
            Items: [new CreateInvoiceItemRequest("Item 1", 100, 1)]
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoices", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvoice_EmptyItems_Returns400()
    {
        // Arrange
        var request = new CreateInvoiceRequest(
            Customer: new CustomerInfo(Guid.NewGuid(), "Customer Name", "123", "a@b.com", "Address"),
            Items: []
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/invoices", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
