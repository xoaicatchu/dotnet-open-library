namespace OrderProcessingSerilog.Api.Models;

public record CreateOrderRequest(
    string CustomerId,
    decimal Amount,
    string ItemName
);

public record OrderResponse(
    string OrderId,
    string CustomerId,
    decimal Amount,
    string ItemName,
    DateTime CreatedAt
);
