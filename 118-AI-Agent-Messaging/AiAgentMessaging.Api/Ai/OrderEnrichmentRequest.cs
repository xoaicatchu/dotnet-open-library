namespace AiAgentMessaging.Api.Ai;

public record OrderEnrichmentRequest(
    Guid OrderId,
    string CustomerName,
    string Product,
    int Quantity,
    decimal TotalPrice);
