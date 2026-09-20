namespace StateMachineDemo.Api.Models;

public enum OrderStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected,
    Cancelled
}

public enum OrderTrigger
{
    Submit,
    StartReview,
    Approve,
    Reject,
    Cancel
}

public record CreateOrderRequest(string CustomerName, decimal TotalAmount);

public record FireTriggerRequest(OrderTrigger Trigger, string? Reason = null);

public record OrderHistoryItem(
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    OrderTrigger Trigger,
    string? Reason,
    DateTime Timestamp);

public record OrderDto(
    string Id,
    string CustomerName,
    decimal TotalAmount,
    OrderStatus Status,
    IReadOnlyList<OrderTrigger> PermittedTriggers,
    IReadOnlyList<OrderHistoryItem> History,
    DateTime CreatedAt);
