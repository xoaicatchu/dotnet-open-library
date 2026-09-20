using NServiceBus;
using System;

namespace OrderBilling.Api.Messages;

public record BillOrderCommand(Guid OrderId, decimal Amount, string CustomerEmail) : ICommand;
public record OrderBilledEvent(Guid OrderId, string InvoiceNumber, DateTime BilledAt) : IEvent;
