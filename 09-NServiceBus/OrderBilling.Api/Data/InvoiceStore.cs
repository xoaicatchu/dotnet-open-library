using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using OrderBilling.Api.Models;

namespace OrderBilling.Api.Data;

public class InvoiceStore
{
    private readonly ConcurrentDictionary<Guid, Invoice> _invoices = new();

    public Invoice CreateInvoice(Guid orderId, decimal amount, string customerEmail)
    {
        var invoice = new Invoice
        {
            OrderId = orderId,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{orderId.ToString().Substring(0, 4).ToUpper()}",
            Amount = amount,
            CustomerEmail = customerEmail,
            Status = "Paid",
            CreatedAt = DateTime.UtcNow
        };

        _invoices.AddOrUpdate(orderId, invoice, (_, _) => invoice);
        return invoice;
    }

    public Invoice? GetByOrderId(Guid orderId)
    {
        _invoices.TryGetValue(orderId, out var invoice);
        return invoice;
    }

    public IEnumerable<Invoice> GetAll()
    {
        return _invoices.Values;
    }
}
