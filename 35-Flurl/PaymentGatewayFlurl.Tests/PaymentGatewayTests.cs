using System.Net;
using System.Net.Http.Json;
using Flurl.Http.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using PaymentGatewayFlurl.Api.Models;

namespace PaymentGatewayFlurl.Tests;

[Collection("FlurlTests")]
public class PaymentGatewayTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PaymentGatewayTests(WebApplicationFactory<Program> factory)
    {
        factory.Server.PreserveExecutionContext = true;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Charge_SuccessfulTransaction_ReturnsOk()
    {
        using var httpTest = new HttpTest();
        var mockResponse = new TransactionResponse(
            TransactionId: "txn_test_12345",
            Status: "Succeeded",
            Amount: 100.00m,
            Currency: "USD",
            Description: "Demo charge",
            CreatedAt: DateTime.UtcNow
        );
        httpTest.RespondWithJson(mockResponse, 200);

        var request = new ChargeRequest("cust_001", 100.00m, "USD", "Demo charge");
        var response = await _client.PostAsJsonAsync("/api/paymentgateway/charges", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(result);
        Assert.Equal("txn_test_12345", result.TransactionId);
        Assert.Equal("Succeeded", result.Status);

        httpTest.ShouldHaveCalled("*/charges")
            .WithVerb(HttpMethod.Post)
            .WithOAuthBearerToken("sk_test_mock_secret_key_12345");
    }

    [Fact]
    public async Task Charge_DeclinedByGateway_ReturnsBadRequestWithGatewayError()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWithJson(new GatewayErrorResponse("CARD_DECLINED", "Card has insufficient funds."), 400);

        var request = new ChargeRequest("cust_001", 500.00m, "USD", "Declined charge");
        var response = await _client.PostAsJsonAsync("/api/paymentgateway/charges", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<GatewayErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("CARD_DECLINED", error.ErrorCode);
    }

    [Fact]
    public async Task Charge_ZeroOrNegativeAmount_ReturnsBadRequestWithoutCallingGateway()
    {
        using var httpTest = new HttpTest();
        var request = new ChargeRequest("cust_001", -10.00m, "USD", "Invalid charge");
        var response = await _client.PostAsJsonAsync("/api/paymentgateway/charges", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        httpTest.ShouldNotHaveCalled("*/charges");
    }

    [Fact]
    public async Task GetTransaction_ExistingId_ReturnsTransaction()
    {
        using var httpTest = new HttpTest();
        var mockResponse = new TransactionResponse(
            TransactionId: "txn_found_999",
            Status: "Succeeded",
            Amount: 75.50m,
            Currency: "USD",
            Description: "Found txn",
            CreatedAt: DateTime.UtcNow
        );
        httpTest.RespondWithJson(mockResponse, 200);

        var response = await _client.GetAsync("/api/paymentgateway/transactions/txn_found_999");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(result);
        Assert.Equal("txn_found_999", result.TransactionId);
        httpTest.ShouldHaveCalled("*/charges/txn_found_999").WithVerb(HttpMethod.Get);
    }

    [Fact]
    public async Task GetTransaction_NonExistent_ReturnsNotFound()
    {
        using var httpTest = new HttpTest();
        httpTest.RespondWith("Not found", 404);

        var response = await _client.GetAsync("/api/paymentgateway/transactions/txn_missing");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Refund_SuccessfulRequest_ReturnsOk()
    {
        using var httpTest = new HttpTest();
        var mockResponse = new TransactionResponse(
            TransactionId: "txn_refund_111",
            Status: "Refunded",
            Amount: 30.00m,
            Currency: "USD",
            Description: "Refund processed",
            CreatedAt: DateTime.UtcNow
        );
        httpTest.RespondWithJson(mockResponse, 200);

        var request = new RefundRequest("txn_original_000", 30.00m, "Customer return");
        var response = await _client.PostAsJsonAsync("/api/paymentgateway/refunds", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        Assert.NotNull(result);
        Assert.Equal("Refunded", result.Status);

        httpTest.ShouldHaveCalled("*/refunds").WithVerb(HttpMethod.Post);
    }

    [Fact]
    public async Task Refund_InvalidAmount_ReturnsBadRequest()
    {
        using var httpTest = new HttpTest();
        var request = new RefundRequest("txn_original_000", 0m, "Customer return");
        var response = await _client.PostAsJsonAsync("/api/paymentgateway/refunds", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        httpTest.ShouldNotHaveCalled("*/refunds");
    }

    [Fact]
    public async Task GetRates_WithQueryParams_PassesCorrectUrlAndParams()
    {
        using var httpTest = new HttpTest();
        var mockRates = new ExchangeRatesResponse(
            BaseCurrency: "USD",
            Rates: new Dictionary<string, decimal>
            {
                { "EUR", 0.92m },
                { "GBP", 0.79m },
                { "JPY", 155.2m }
            },
            Date: DateTime.UtcNow
        );
        httpTest.RespondWithJson(mockRates, 200);

        var response = await _client.GetAsync("/api/paymentgateway/rates?baseCurrency=USD&symbols=EUR,GBP,JPY");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rates = await response.Content.ReadFromJsonAsync<ExchangeRatesResponse>();
        Assert.NotNull(rates);
        Assert.Equal("USD", rates.BaseCurrency);
        Assert.Equal(0.92m, rates.Rates["EUR"]);

        httpTest.ShouldHaveCalled("*/rates*")
            .WithQueryParam("base", "USD")
            .WithQueryParam("symbols", "EUR,GBP,JPY");
    }
}
