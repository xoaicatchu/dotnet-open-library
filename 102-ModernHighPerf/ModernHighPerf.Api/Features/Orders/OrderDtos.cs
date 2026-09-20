namespace ModernHighPerf.Api.Features.Orders;

public record OrderDto(int Id, string CustomerName, string CustomerEmail, decimal TotalAmount, string Status, DateTime CreatedAt, List<OrderItemDto> Items);
public record OrderItemDto(int Id, string ProductName, int Quantity, decimal UnitPrice);

public record CreateOrderRequest(string CustomerName, string CustomerEmail, List<CreateOrderItemRequest> Items);
public record CreateOrderItemRequest(string ProductName, int Quantity, decimal UnitPrice);

public record GetOrdersQuery;
public record CreateOrderCommand(CreateOrderRequest Request);
