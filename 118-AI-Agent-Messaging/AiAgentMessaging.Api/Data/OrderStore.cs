using System.Collections.Concurrent;
using AiAgentMessaging.Api.Models;

namespace AiAgentMessaging.Api.Data;

public class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public void Add(Order order) => _orders[order.Id] = order;

    public Order? GetById(Guid id)
    {
        _orders.TryGetValue(id, out var order);
        return order;
    }

    public IEnumerable<Order> GetAll() => _orders.Values;

    public void Update(Guid id, Action<Order> updateAction)
    {
        if (_orders.TryGetValue(id, out var order))
        {
            updateAction(order);
            order.UpdatedAt = DateTime.UtcNow;
        }
    }
}
