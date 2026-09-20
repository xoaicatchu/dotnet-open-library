namespace ResilientPaymentGateway.Api.Models;

public enum GatewayBehavior
{
    Success,
    TransientFailure, // Fails twice, succeeds on 3rd attempt
    PermanentFailure, // Always throws HttpRequestException
    Timeout // Delays 2 seconds to exceed timeout
}

public record PaymentRequest(
    string AccountId,
    decimal Amount,
    string Currency
);

public record PaymentResponse(
    string TransactionId,
    string Status,
    int Attempts,
    bool IsFallback,
    string Message
);

public record GatewayStatusDto(
    int TotalAttempts,
    GatewayBehavior CurrentBehavior
);
