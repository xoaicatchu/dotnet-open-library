namespace CleanVerticalSlice.Application.Orders.GetOrders;

using System.Collections.Generic;
using MediatR;

public record GetOrdersQuery : IRequest<List<OrderSummaryDto>>;
