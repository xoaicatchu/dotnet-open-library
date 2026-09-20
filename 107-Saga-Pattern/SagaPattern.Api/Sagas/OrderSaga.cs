using System;
using MassTransit;
using SagaPattern.Api.Messages;

namespace SagaPattern.Api.Sagas;

public class OrderSaga : MassTransitStateMachine<OrderSagaState>
{
    public State Placed { get; private set; } = null!;
    public State PaymentProcessing { get; private set; } = null!;
    public State InventoryReserving { get; private set; } = null!;
    public State Shipping { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<OrderPlaced> OrderPlacedEvent { get; private set; } = null!;
    public Event<PaymentProcessed> PaymentProcessedEvent { get; private set; } = null!;
    public Event<PaymentFailed> PaymentFailedEvent { get; private set; } = null!;
    public Event<InventoryReserved> InventoryReservedEvent { get; private set; } = null!;
    public Event<InventoryFailed> InventoryFailedEvent { get; private set; } = null!;
    public Event<OrderShipped> OrderShippedEvent { get; private set; } = null!;

    public OrderSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderPlacedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentProcessedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => PaymentFailedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => InventoryReservedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => InventoryFailedEvent, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => OrderShippedEvent, x => x.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderPlacedEvent)
                .Then(ctx => {
                    ctx.Saga.CustomerName = ctx.Message.CustomerName;
                    ctx.Saga.TotalAmount = ctx.Message.TotalAmount;
                    ctx.Saga.PlacedAt = DateTime.UtcNow;
                })
                .Publish(ctx => new ProcessPaymentCommand(ctx.Message.OrderId, ctx.Message.TotalAmount))
                .TransitionTo(PaymentProcessing));

        During(PaymentProcessing,
            When(PaymentProcessedEvent)
                .Publish(ctx => new ReserveInventoryCommand(ctx.Message.OrderId, "Product", 1))
                .TransitionTo(InventoryReserving),
            When(PaymentFailedEvent)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .TransitionTo(Failed));

        During(InventoryReserving,
            When(InventoryReservedEvent)
                .Publish(ctx => new ShipOrderCommand(ctx.Message.OrderId, ctx.Saga.CustomerName))
                .TransitionTo(Shipping),
            When(InventoryFailedEvent)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .TransitionTo(Failed));

        During(Shipping,
            When(OrderShippedEvent)
                .Then(ctx => ctx.Saga.CompletedAt = DateTime.UtcNow)
                .TransitionTo(Completed));

        
    }
}

