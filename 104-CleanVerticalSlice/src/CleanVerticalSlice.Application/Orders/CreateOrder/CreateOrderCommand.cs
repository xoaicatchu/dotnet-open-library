namespace CleanVerticalSlice.Application.Orders.CreateOrder;

using CleanVerticalSlice.Application.Orders.GetOrders;
using MediatR;

public record CreateOrderCommand(CreateOrderRequest Request) : IRequest<OrderSummaryDto>;
