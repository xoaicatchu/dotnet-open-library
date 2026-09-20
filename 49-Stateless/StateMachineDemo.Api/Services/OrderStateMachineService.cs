using System.Collections.Concurrent;
using Stateless;
using Stateless.Graph;
using StateMachineDemo.Api.Models;

namespace StateMachineDemo.Api.Services;

public class OrderEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CustomerName { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public List<OrderHistoryItem> History { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderStateMachineService
{
    private readonly ConcurrentDictionary<string, OrderEntity> _orders = new();

    public OrderDto CreateOrder(string customerName, decimal amount)
    {
        var order = new OrderEntity
        {
            CustomerName = customerName,
            TotalAmount = amount,
            Status = OrderStatus.Draft
        };

        _orders[order.Id] = order;
        return MapToDto(order);
    }

    public OrderDto? GetOrder(string id)
    {
        if (!_orders.TryGetValue(id, out var order))
            return null;

        return MapToDto(order);
    }

    public (bool Success, string? Error, OrderDto? Order) FireTrigger(string id, OrderTrigger trigger, string? reason = null)
    {
        if (!_orders.TryGetValue(id, out var order))
            return (false, "Order not found", null);

        var machine = BuildStateMachine(order, reason);

        if (!machine.CanFire(trigger))
        {
            var permitted = string.Join(", ", machine.PermittedTriggers);
            return (false, $"Cannot transition with trigger '{trigger}' from state '{order.Status}'. Permitted triggers: [{permitted}]", null);
        }

        try
        {
            machine.Fire(trigger);
            return (true, null, MapToDto(order));
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public string? GetGraph(string id, string format = "dot")
    {
        if (!_orders.TryGetValue(id, out var order))
            return null;

        var machine = BuildStateMachine(order);
        var info = machine.GetInfo();

        return format.ToLowerInvariant() switch
        {
            "mermaid" => MermaidGraph.Format(info),
            _ => UmlDotGraph.Format(info)
        };
    }

    public StateMachine<OrderStatus, OrderTrigger> BuildStateMachine(OrderEntity order, string? reason = null)
    {
        var machine = new StateMachine<OrderStatus, OrderTrigger>(
            () => order.Status,
            s => order.Status = s);

        // Configure Draft
        machine.Configure(OrderStatus.Draft)
            .Permit(OrderTrigger.Submit, OrderStatus.Submitted)
            .Permit(OrderTrigger.Cancel, OrderStatus.Cancelled);

        // Configure Submitted
        machine.Configure(OrderStatus.Submitted)
            .Permit(OrderTrigger.StartReview, OrderStatus.UnderReview)
            .Permit(OrderTrigger.Cancel, OrderStatus.Cancelled);

        // Configure UnderReview
        machine.Configure(OrderStatus.UnderReview)
            .Permit(OrderTrigger.Approve, OrderStatus.Approved)
            .Permit(OrderTrigger.Reject, OrderStatus.Rejected)
            .Permit(OrderTrigger.Cancel, OrderStatus.Cancelled);

        // Terminal states
        machine.Configure(OrderStatus.Approved)
            .Ignore(OrderTrigger.Approve);

        machine.Configure(OrderStatus.Rejected);
        machine.Configure(OrderStatus.Cancelled);

        // On Transitioned Callback for audit history
        machine.OnTransitioned(transition =>
        {
            order.History.Add(new OrderHistoryItem(
                transition.Source,
                transition.Destination,
                transition.Trigger,
                reason,
                DateTime.UtcNow));
        });

        return machine;
    }

    private OrderDto MapToDto(OrderEntity order)
    {
        var machine = BuildStateMachine(order);
        var permitted = machine.PermittedTriggers.ToList();

        return new OrderDto(
            order.Id,
            order.CustomerName,
            order.TotalAmount,
            order.Status,
            permitted,
            order.History.AsReadOnly(),
            order.CreatedAt);
    }
}
