namespace AiAgentMessaging.Api.Models;

public record CreateOrderRequest(string CustomerName, string Product, int Quantity, decimal TotalPrice);
