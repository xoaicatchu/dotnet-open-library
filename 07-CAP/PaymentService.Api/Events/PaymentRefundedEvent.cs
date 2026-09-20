namespace PaymentService.Api.Events;

public record PaymentRefundedEvent(Guid PaymentId, string Reason);
