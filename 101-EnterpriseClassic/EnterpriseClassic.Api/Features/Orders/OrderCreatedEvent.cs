namespace EnterpriseClassic.Api.Features.Orders;

public record OrderCreatedEvent(int OrderId, string CustomerEmail);
