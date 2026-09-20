using DotNetCore.CAP;
using Microsoft.Extensions.Logging;
using PaymentService.Api.Data;
using PaymentService.Api.Events;

namespace PaymentService.Api.Subscribers;

public class PaymentEventSubscriber : ICapSubscribe
{
    private readonly PaymentStore _store;
    private readonly ILogger<PaymentEventSubscriber> _logger;
    
    public PaymentEventSubscriber(PaymentStore store, ILogger<PaymentEventSubscriber> logger)
    {
        _store = store;
        _logger = logger;
    }
    
    [CapSubscribe("payment.created")]
    public void HandlePaymentCreated(PaymentCreatedEvent @event)
    {
        _logger.LogInformation("Payment {PaymentId} created for order {OrderId}", @event.PaymentId, @event.OrderId);
        _store.UpdateStatus(@event.PaymentId, "Completed");
    }
    
    [CapSubscribe("payment.refunded")]
    public void HandlePaymentRefunded(PaymentRefundedEvent @event)
    {
        _logger.LogInformation("Payment {PaymentId} refunded", @event.PaymentId);
        _store.UpdateStatus(@event.PaymentId, "Refunded");
    }
}
