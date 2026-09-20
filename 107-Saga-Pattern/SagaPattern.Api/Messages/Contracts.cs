using System;
using MassTransit;

namespace SagaPattern.Api.Messages;

public record PlaceOrderCommand(Guid OrderId, string CustomerName, decimal TotalAmount);
public record ProcessPaymentCommand(Guid OrderId, decimal Amount);
public record ReserveInventoryCommand(Guid OrderId, string ProductName, int Quantity);
public record ShipOrderCommand(Guid OrderId, string CustomerName);

public record OrderPlaced(Guid OrderId, string CustomerName, decimal TotalAmount) : CorrelatedBy<Guid> { public Guid CorrelationId => OrderId; }
public record PaymentProcessed(Guid OrderId) : CorrelatedBy<Guid> { public Guid CorrelationId => OrderId; }
public record PaymentFailed(Guid OrderId, string Reason) : CorrelatedBy<Guid> { public Guid CorrelationId => OrderId; }
public record InventoryReserved(Guid OrderId) : CorrelatedBy<Guid> { public Guid CorrelationId => OrderId; }
public record InventoryFailed(Guid OrderId, string Reason) : CorrelatedBy<Guid> { public Guid CorrelationId => OrderId; }
public record OrderShipped(Guid OrderId) : CorrelatedBy<Guid> { public Guid CorrelationId => OrderId; }
public record OrderCompleted(Guid OrderId) : CorrelatedBy<Guid> { public Guid CorrelationId => OrderId; }
