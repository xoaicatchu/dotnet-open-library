using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderBilling.Api.Models;
using Xunit;

namespace OrderBilling.Tests;

public class BillingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BillingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SubmitBilling_ValidRequest_ReturnsAcceptedAndGeneratesInvoice()
    {
        // Arrange
        var request = new
        {
            orderId = Guid.NewGuid(),
            amount = 1500000m,
            customerEmail = "customer@example.com"
        };

        // Act 1: Submit billing command
        var response = await _client.PostAsJsonAsync("/api/billing/bill", request);

        // Assert 1: Returns 202 Accepted
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        // Wait for NServiceBus message to be processed
        await Task.Delay(2000);

        // Act 2: Get invoice
        var getResponse = await _client.GetAsync($"/api/billing/invoices/{request.orderId}");

        // Assert 2: Returns 200 OK and "Paid" status
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var invoice = await getResponse.Content.ReadFromJsonAsync<Invoice>();
        Assert.NotNull(invoice);
        Assert.Equal("Paid", invoice.Status);
        Assert.Equal(request.amount, invoice.Amount);
    }

    [Fact]
    public async Task GetInvoices_ReturnsList()
    {
        var response = await _client.GetAsync("/api/billing/invoices");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await response.Content.ReadFromJsonAsync<List<Invoice>>();
        Assert.NotNull(list);
    }

    [Fact]
    public async Task SubmitBilling_InvalidAmount_ReturnsBadRequest()
    {
        var request = new
        {
            orderId = Guid.NewGuid(),
            amount = 0m,
            customerEmail = "customer@example.com"
        };

        var response = await _client.PostAsJsonAsync("/api/billing/bill", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitBilling_EmptyEmail_ReturnsBadRequest()
    {
        var request = new
        {
            orderId = Guid.NewGuid(),
            amount = 10m,
            customerEmail = ""
        };

        var response = await _client.PostAsJsonAsync("/api/billing/bill", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoice_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/billing/invoices/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSwagger_ReturnsOk()
    {
        var response = await _client.GetAsync("/swagger/index.html");
        Assert.True(response.IsSuccessStatusCode);
    }
}
