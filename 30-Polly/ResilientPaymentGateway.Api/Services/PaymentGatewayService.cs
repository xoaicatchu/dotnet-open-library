using ResilientPaymentGateway.Api.Models;

namespace ResilientPaymentGateway.Api.Services;

public interface IPaymentGatewayService
{
    Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken);
    void SetBehavior(GatewayBehavior behavior);
    GatewayBehavior CurrentBehavior { get; }
    int TotalAttempts { get; }
    void Reset();
}

public class PaymentGatewayService : IPaymentGatewayService
{
    private int _attempts;
    private GatewayBehavior _behavior = GatewayBehavior.Success;

    public int TotalAttempts => _attempts;
    public GatewayBehavior CurrentBehavior => _behavior;

    public void SetBehavior(GatewayBehavior behavior)
    {
        _behavior = behavior;
    }

    public void Reset()
    {
        Interlocked.Exchange(ref _attempts, 0);
        _behavior = GatewayBehavior.Success;
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        var currentAttempt = Interlocked.Increment(ref _attempts);

        switch (_behavior)
        {
            case GatewayBehavior.Success:
                return new PaymentResponse(
                    TransactionId: Guid.NewGuid().ToString("N"),
                    Status: "Completed",
                    Attempts: currentAttempt,
                    IsFallback: false,
                    Message: "Payment processed successfully."
                );

            case GatewayBehavior.TransientFailure:
                // Fails twice, succeeds on 3rd attempt
                if (currentAttempt < 3)
                {
                    throw new HttpRequestException($"Gateway transient network error (attempt {currentAttempt})");
                }
                return new PaymentResponse(
                    TransactionId: Guid.NewGuid().ToString("N"),
                    Status: "CompletedAfterRetry",
                    Attempts: currentAttempt,
                    IsFallback: false,
                    Message: "Payment processed successfully after transient error recovery."
                );

            case GatewayBehavior.PermanentFailure:
                throw new HttpRequestException("Gateway completely unreachable (500 Internal Server Error)");

            case GatewayBehavior.Timeout:
                // Delay 2000ms to exceed pipeline 300ms timeout
                await Task.Delay(2000, cancellationToken);
                return new PaymentResponse(
                    TransactionId: Guid.NewGuid().ToString("N"),
                    Status: "Completed",
                    Attempts: currentAttempt,
                    IsFallback: false,
                    Message: "Delayed response."
                );

            default:
                throw new InvalidOperationException("Unknown behavior.");
        }
    }
}
