namespace OrderProcessor.Api.Contracts;

public record OrderProcessed(Guid OrderId, DateTime ProcessedAt);
