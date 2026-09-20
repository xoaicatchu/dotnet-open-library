using System.Collections.Concurrent;
using PaymentService.Api.Models;

namespace PaymentService.Api.Data;

public class PaymentStore
{
    private readonly ConcurrentDictionary<Guid, Payment> _payments = new();

    public Payment Add(string orderId, decimal amount, string currency)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Amount = amount,
            Currency = currency,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _payments[payment.Id] = payment;
        return payment;
    }

    public Payment? GetById(Guid id)
    {
        _payments.TryGetValue(id, out var payment);
        return payment;
    }

    public IEnumerable<Payment> GetAll()
    {
        return _payments.Values.OrderByDescending(p => p.CreatedAt);
    }

    public bool UpdateStatus(Guid id, string status)
    {
        if (_payments.TryGetValue(id, out var payment))
        {
            payment.Status = status;
            if (status == "Completed" || status == "Refunded")
            {
                payment.CompletedAt = DateTime.UtcNow;
            }
            return true;
        }
        return false;
    }
}
