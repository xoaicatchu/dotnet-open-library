namespace PaymentGatewayFlurl.Api.Models;

public record ChargeRequest(
    string CustomerId,
    decimal Amount,
    string Currency,
    string Description
);

public record RefundRequest(
    string TransactionId,
    decimal Amount,
    string Reason
);

public record TransactionResponse(
    string TransactionId,
    string Status,
    decimal Amount,
    string Currency,
    string Description,
    DateTime CreatedAt
);

public record ExchangeRatesResponse(
    string BaseCurrency,
    Dictionary<string, decimal> Rates,
    DateTime Date
);

public record GatewayErrorResponse(
    string ErrorCode,
    string Message
);
