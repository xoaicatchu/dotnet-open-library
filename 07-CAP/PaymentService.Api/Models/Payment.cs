namespace PaymentService.Api.Models;

public class Payment
{
    public Guid Id { get; set; }
    public string OrderId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public record CreatePaymentRequest(string OrderId, decimal Amount, string Currency);
public record RefundPaymentRequest(string Reason);

public record PaymentResponse(Guid Id, string OrderId, decimal Amount, string Currency, string Status, DateTime CreatedAt, DateTime? CompletedAt)
{
    public static PaymentResponse From(Payment payment)
    {
        return new PaymentResponse(payment.Id, payment.OrderId, payment.Amount, payment.Currency, payment.Status, payment.CreatedAt, payment.CompletedAt);
    }
}
