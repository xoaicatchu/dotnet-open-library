namespace AiAgentMessaging.Api.Contracts;

public record OrderSubmitted(Guid OrderId, string CustomerName, string Product, int Quantity, decimal TotalPrice);
