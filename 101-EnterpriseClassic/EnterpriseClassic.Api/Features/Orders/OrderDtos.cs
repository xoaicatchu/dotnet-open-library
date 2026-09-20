using AutoMapper;

namespace EnterpriseClassic.Api.Features.Orders;

public record OrderSummaryDto
{
    public int Id { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public double TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
}
public record OrderDto(int Id, string CustomerName, string CustomerEmail, decimal TotalAmount, string Status, DateTime CreatedAt, List<OrderItemDto> Items);
public record OrderItemDto(int Id, string ProductName, int Quantity, decimal UnitPrice);
public record CreateOrderRequest(string CustomerName, string CustomerEmail, List<CreateOrderItemRequest> Items);
public record CreateOrderItemRequest(string ProductName, int Quantity, decimal UnitPrice);

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<OrderEntity, OrderSummaryDto>();
        CreateMap<OrderEntity, OrderDto>();
        CreateMap<OrderItemEntity, OrderItemDto>();
    }
}
