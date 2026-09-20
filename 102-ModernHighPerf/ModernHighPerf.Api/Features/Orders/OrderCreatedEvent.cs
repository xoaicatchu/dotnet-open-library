namespace ModernHighPerf.Api.Features.Orders;

public record OrderCreatedEvent(int OrderId, string CustomerEmail);
