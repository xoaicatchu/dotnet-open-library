namespace OrderManagement.Api.Models;

public record CreateOrderItemDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record CreateOrderDto(
    string CustomerName,
    string ShippingAddress,
    List<CreateOrderItemDto> Items
);

public record UpdateOrderStatusDto(
    string Status
);

public record OrderItemDto(
    int Id,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal
);

public record OrderSummaryDto(
    int Id,
    string CustomerName,
    string ShippingAddress,
    string Status,
    decimal TotalAmount,
    int ItemCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record OrderDetailDto(
    int Id,
    string CustomerName,
    string ShippingAddress,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<OrderItemDto> Items
);

public record OrderStatisticsDto(
    int TotalOrders,
    decimal TotalRevenue,
    int TotalItemsSold,
    Dictionary<string, int> OrdersByStatus
);
