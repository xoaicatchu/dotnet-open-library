using PaymentGatewayFlurl.Api.Models;

namespace PaymentGatewayFlurl.Api.Services;

public interface IPaymentGatewayService
{
    Task<TransactionResponse> ChargeAsync(ChargeRequest request);
    Task<TransactionResponse?> GetTransactionAsync(string transactionId);
    Task<TransactionResponse> RefundAsync(RefundRequest request);
    Task<ExchangeRatesResponse> GetRatesAsync(string baseCurrency, string[] symbols);
}
