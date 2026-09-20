using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PaymentService.Api.Models;
using Xunit;

namespace PaymentService.Tests;

public class PaymentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PaymentTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePayment_Returns201_AndEventuallyCompletes()
    {
        // 1. Create payment
        var req = new CreatePaymentRequest("ORD-123", 100.50m, "VND");
        var res = await _client.PostAsJsonAsync("/api/payments", req);
        
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var payment = await res.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);
        Assert.Equal("Pending", payment.Status); // initially pending
        
        // 2. Get payment by ID
        var getRes = await _client.GetAsync($"/api/payments/{payment.Id}");
        Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
        
        // 4. Wait for CAP subscriber to process and change status
        await Task.Delay(1000); // 1 second delay for in-memory CAP processing
        
        var completedRes = await _client.GetFromJsonAsync<PaymentResponse>($"/api/payments/{payment.Id}");
        Assert.NotNull(completedRes);
        Assert.Equal("Completed", completedRes.Status);
    }

    [Fact]
    public async Task ListPayments_Returns200()
    {
        var req = new CreatePaymentRequest("ORD-222", 50, "USD");
        await _client.PostAsJsonAsync("/api/payments", req);

        var res = await _client.GetAsync("/api/payments");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var payments = await res.Content.ReadFromJsonAsync<PaymentResponse[]>();
        Assert.NotNull(payments);
        Assert.NotEmpty(payments);
    }

    [Fact]
    public async Task RefundPayment_ChangesStatus()
    {
        var req = new CreatePaymentRequest("ORD-333", 500, "VND");
        var createRes = await _client.PostAsJsonAsync("/api/payments", req);
        var payment = await createRes.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.NotNull(payment);

        var refundReq = new RefundPaymentRequest("Customer request");
        var refundRes = await _client.PostAsJsonAsync($"/api/payments/{payment.Id}/refund", refundReq);
        Assert.Equal(HttpStatusCode.OK, refundRes.StatusCode);

        // Wait for CAP processing
        await Task.Delay(1000);

        var getRes = await _client.GetFromJsonAsync<PaymentResponse>($"/api/payments/{payment.Id}");
        Assert.NotNull(getRes);
        Assert.Equal("Refunded", getRes.Status);
    }

    [Fact]
    public async Task CreatePayment_InvalidData_Returns400()
    {
        // Empty OrderId
        var req1 = new CreatePaymentRequest("", 100, "USD");
        var res1 = await _client.PostAsJsonAsync("/api/payments", req1);
        Assert.Equal(HttpStatusCode.BadRequest, res1.StatusCode);

        // Negative Amount
        var req2 = new CreatePaymentRequest("ORD", -10, "USD");
        var res2 = await _client.PostAsJsonAsync("/api/payments", req2);
        Assert.Equal(HttpStatusCode.BadRequest, res2.StatusCode);

        // Invalid Currency
        var req3 = new CreatePaymentRequest("ORD", 100, "VNDD");
        var res3 = await _client.PostAsJsonAsync("/api/payments", req3);
        Assert.Equal(HttpStatusCode.BadRequest, res3.StatusCode);
    }

    [Fact]
    public async Task GetPayment_NonExistent_Returns404()
    {
        var res = await _client.GetAsync($"/api/payments/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsAvailable()
    {
        var res = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
