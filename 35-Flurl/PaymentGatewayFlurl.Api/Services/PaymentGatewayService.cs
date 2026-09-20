using Flurl;
using Flurl.Http;
using PaymentGatewayFlurl.Api.Models;

namespace PaymentGatewayFlurl.Api.Services;

public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly string _baseUrl;
    private readonly string _apiKey;
    private readonly ILogger<PaymentGatewayService> _logger;

    public PaymentGatewayService(IConfiguration configuration, ILogger<PaymentGatewayService> logger)
    {
        _baseUrl = configuration["PaymentGateway:BaseUrl"] ?? "https://api.stripe-mock.internal/v1";
        _apiKey = configuration["PaymentGateway:ApiKey"] ?? "sk_test_default";
        _logger = logger;
    }

    public async Task<TransactionResponse> ChargeAsync(ChargeRequest request)
    {
        return await _baseUrl
            .AppendPathSegment("charges")
            .WithOAuthBearerToken(_apiKey)
            .WithHeader("Accept", "application/json")
            .PostJsonAsync(request)
            .ReceiveJson<TransactionResponse>();
    }

    public async Task<TransactionResponse?> GetTransactionAsync(string transactionId)
    {
        try
        {
            return await _baseUrl
                .AppendPathSegments("charges", transactionId)
                .WithOAuthBearerToken(_apiKey)
                .GetJsonAsync<TransactionResponse>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("Transaction {TransactionId} not found on gateway", transactionId);
            return null;
        }
    }

    public async Task<TransactionResponse> RefundAsync(RefundRequest request)
    {
        return await _baseUrl
            .AppendPathSegment("refunds")
            .WithOAuthBearerToken(_apiKey)
            .PostJsonAsync(request)
            .ReceiveJson<TransactionResponse>();
    }

    public async Task<ExchangeRatesResponse> GetRatesAsync(string baseCurrency, string[] symbols)
    {
        return await _baseUrl
            .AppendPathSegment("rates")
            .SetQueryParam("base", baseCurrency)
            .SetQueryParam("symbols", string.Join(",", symbols))
            .WithOAuthBearerToken(_apiKey)
            .GetJsonAsync<ExchangeRatesResponse>();
    }
}
