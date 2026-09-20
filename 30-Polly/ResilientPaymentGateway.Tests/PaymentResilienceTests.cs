using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using ResilientPaymentGateway.Api.Models;

namespace ResilientPaymentGateway.Tests;

public class PaymentResilienceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PaymentResilienceTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Process_Success_ReturnsSuccessWithOneAttempt()
    {
        // Reset
        await _client.PostAsync("/api/payments/reset", null);

        var request = new PaymentRequest("ACC-001", 100m, "USD");
        var response = await _client.PostAsJsonAsync("/api/payments/process", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(data);
        Assert.Equal("Completed", data.Status);
        Assert.Equal(1, data.Attempts);
        Assert.False(data.IsFallback);
    }

    [Fact]
    public async Task Process_TransientFailure_RetriesAndSucceeds()
    {
        // Reset
        await _client.PostAsync("/api/payments/reset", null);

        // Configure transient failure
        await _client.PostAsync("/api/payments/gateway-behavior?behavior=TransientFailure", null);

        var request = new PaymentRequest("ACC-002", 250m, "USD");
        var response = await _client.PostAsJsonAsync("/api/payments/process", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(data);
        Assert.Equal("CompletedAfterRetry", data.Status);
        Assert.Equal(3, data.Attempts); // Retried until attempt 3 succeeded
        Assert.False(data.IsFallback);
    }

    [Fact]
    public async Task Process_PermanentFailure_TriggersFallback()
    {
        // Reset
        await _client.PostAsync("/api/payments/reset", null);

        // Configure permanent failure
        await _client.PostAsync("/api/payments/gateway-behavior?behavior=PermanentFailure", null);

        var request = new PaymentRequest("ACC-003", 500m, "USD");
        var response = await _client.PostAsJsonAsync("/api/payments/process", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(data);
        Assert.Equal("QueuedForOfflineProcessing", data.Status);
        Assert.True(data.IsFallback);
    }

    [Fact]
    public async Task Process_Timeout_TriggersFallback()
    {
        // Reset
        await _client.PostAsync("/api/payments/reset", null);

        // Configure timeout
        await _client.PostAsync("/api/payments/gateway-behavior?behavior=Timeout", null);

        var request = new PaymentRequest("ACC-004", 75m, "USD");
        var response = await _client.PostAsJsonAsync("/api/payments/process", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(data);
        Assert.Equal("QueuedForOfflineProcessing", data.Status);
        Assert.True(data.IsFallback);
    }

    [Fact]
    public async Task Process_WithInvalidAmount_ReturnsBadRequest()
    {
        var request = new PaymentRequest("ACC-005", -10m, "USD");
        var response = await _client.PostAsJsonAsync("/api/payments/process", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Process_WithEmptyAccountId_ReturnsBadRequest()
    {
        var request = new PaymentRequest("", 100m, "USD");
        var response = await _client.PostAsJsonAsync("/api/payments/process", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
