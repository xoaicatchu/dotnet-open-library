using System;

namespace OrderWorkflowActors.Api.Messages;

public record CreateOrder(string CustomerName, decimal Amount);
public record ProcessPayment(Guid OrderId, decimal Amount);
public record CancelOrder(Guid OrderId);
public record GetOrderStatus(Guid OrderId);

public record OrderCreated(Guid OrderId, string Status);
public record OrderDetails(Guid OrderId, string CustomerName, decimal Amount, string Status, DateTime CreatedAt);
public record OrderError(string Message);
