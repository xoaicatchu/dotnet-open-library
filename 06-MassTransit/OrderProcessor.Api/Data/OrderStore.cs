using System.Collections.Concurrent;
using OrderProcessor.Api.Models;

namespace OrderProcessor.Api.Data;

public class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public void Add(Order order)
    {
        _orders[order.Id] = order;
    }

    public Order? GetById(Guid id)
    {
        _orders.TryGetValue(id, out var order);
        return order;
    }

    public IEnumerable<Order> GetAll()
    {
        return _orders.Values;
    }

    public void UpdateStatus(Guid id, string status)
    {
        if (_orders.TryGetValue(id, out var order))
        {
            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
        }
    }
}
