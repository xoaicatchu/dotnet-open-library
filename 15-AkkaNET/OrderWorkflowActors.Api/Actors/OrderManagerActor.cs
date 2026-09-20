using System;
using System.Collections.Generic;
using Akka.Actor;
using OrderWorkflowActors.Api.Messages;

namespace OrderWorkflowActors.Api.Actors;

public class OrderManagerActor : ReceiveActor
{
    private readonly Dictionary<Guid, OrderState> _orders = new();

    public OrderManagerActor()
    {
        Receive<CreateOrder>(msg =>
        {
            var orderId = Guid.NewGuid();
            var orderState = new OrderState
            {
                OrderId = orderId,
                CustomerName = msg.CustomerName,
                Amount = msg.Amount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _orders[orderId] = orderState;
            
            Sender.Tell(new OrderCreated(orderId, orderState.Status));
        });

        Receive<ProcessPayment>(msg =>
        {
            if (!_orders.TryGetValue(msg.OrderId, out var order))
            {
                Sender.Tell(new OrderError("Order not found"));
                return;
            }

            if (order.Status == "Cancelled" || order.Status == "Paid")
            {
                Sender.Tell(new OrderError("Order cannot be paid in current status"));
                return;
            }

            if (msg.Amount < order.Amount)
            {
                Sender.Tell(new OrderError("Insufficient payment amount"));
                return;
            }

            order.Status = "Paid";
            Sender.Tell(new OrderDetails(order.OrderId, order.CustomerName, order.Amount, order.Status, order.CreatedAt));
        });

        Receive<CancelOrder>(msg =>
        {
            if (!_orders.TryGetValue(msg.OrderId, out var order))
            {
                Sender.Tell(new OrderError("Order not found"));
                return;
            }

            if (order.Status == "Paid")
            {
                Sender.Tell(new OrderError("Paid order cannot be cancelled"));
                return;
            }

            order.Status = "Cancelled";
            Sender.Tell(new OrderDetails(order.OrderId, order.CustomerName, order.Amount, order.Status, order.CreatedAt));
        });

        Receive<GetOrderStatus>(msg =>
        {
            if (_orders.TryGetValue(msg.OrderId, out var order))
            {
                Sender.Tell(new OrderDetails(order.OrderId, order.CustomerName, order.Amount, order.Status, order.CreatedAt));
            }
            else
            {
                Sender.Tell(new OrderError("Order not found"));
            }
        });
    }

    private class OrderState
    {
        public Guid OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
