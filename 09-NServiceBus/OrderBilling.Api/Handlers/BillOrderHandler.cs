using Microsoft.Extensions.Logging;
using NServiceBus;
using OrderBilling.Api.Data;
using OrderBilling.Api.Messages;
using System.Threading.Tasks;

namespace OrderBilling.Api.Handlers;

public class BillOrderHandler : IHandleMessages<BillOrderCommand>
{
    private readonly InvoiceStore _store;
    private readonly ILogger<BillOrderHandler> _logger;

    public BillOrderHandler(InvoiceStore store, ILogger<BillOrderHandler> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task Handle(BillOrderCommand message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Processing billing for Order: {OrderId}", message.OrderId);
        
        var invoice = _store.CreateInvoice(message.OrderId, message.Amount, message.CustomerEmail);
        
        await context.Publish(new OrderBilledEvent(invoice.OrderId, invoice.InvoiceNumber, invoice.CreatedAt));
    }
}
