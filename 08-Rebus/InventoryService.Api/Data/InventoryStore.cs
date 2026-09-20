using System.Collections.Concurrent;
using InventoryService.Api.Models;

namespace InventoryService.Api.Data;

public class InventoryStore
{
    private readonly ConcurrentDictionary<int, InventoryItem> _items = new();
    private int _nextId = 1;

    public InventoryStore()
    {
        // Seed
        var kbd = new InventoryItem { Id = _nextId++, Sku = "KB-001", Name = "Keyboard", Quantity = 100, LastUpdated = DateTime.UtcNow };
        _items.TryAdd(kbd.Id, kbd);
        
        var mouse = new InventoryItem { Id = _nextId++, Sku = "MS-001", Name = "Mouse", Quantity = 200, LastUpdated = DateTime.UtcNow };
        _items.TryAdd(mouse.Id, mouse);
    }

    public IEnumerable<InventoryItem> GetAll() => _items.Values;

    public InventoryItem? GetById(int id) => _items.TryGetValue(id, out var item) ? item : null;

    public InventoryItem Add(InventoryItem item)
    {
        item.Id = Interlocked.Increment(ref _nextId) - 1;
        item.LastUpdated = DateTime.UtcNow;
        _items.TryAdd(item.Id, item);
        return item;
    }

    public InventoryItem? AddStock(int id, int amount)
    {
        if (amount <= 0) return null;
        
        if (_items.TryGetValue(id, out var item))
        {
            lock (item)
            {
                item.Quantity += amount;
                item.LastUpdated = DateTime.UtcNow;
            }
            return item;
        }
        return null;
    }

    public InventoryItem? RemoveStock(int id, int amount)
    {
        if (amount <= 0) return null;

        if (_items.TryGetValue(id, out var item))
        {
            lock (item)
            {
                if (item.Quantity >= amount)
                {
                    item.Quantity -= amount;
                    item.LastUpdated = DateTime.UtcNow;
                    return item;
                }
            }
        }
        return null;
    }
}
