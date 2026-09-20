namespace OrderProcessor.Api.Contracts;

public record OrderCancelled(Guid OrderId, string Reason);
