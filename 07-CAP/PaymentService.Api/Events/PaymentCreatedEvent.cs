namespace PaymentService.Api.Events;

public record PaymentCreatedEvent(Guid PaymentId, string OrderId, decimal Amount, string Currency);
