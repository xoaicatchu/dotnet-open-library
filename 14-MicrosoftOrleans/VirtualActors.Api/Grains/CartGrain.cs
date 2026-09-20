using VirtualActors.Api.Models;

namespace VirtualActors.Api.Grains;

public class CartGrain : Grain, ICartGrain
{
    private readonly List<CartItem> _items = new();

    public Task AddItem(string sku, string name, int quantity, decimal unitPrice)
    {
        var existing = _items.FirstOrDefault(i => i.Sku == sku);
        if (existing != null)
        {
            _items.Remove(existing);
            _items.Add(existing with { Quantity = existing.Quantity + quantity });
        }
        else
        {
            _items.Add(new CartItem(sku, name, quantity, unitPrice));
        }

        return Task.CompletedTask;
    }

    public Task RemoveItem(string sku)
    {
        _items.RemoveAll(i => i.Sku == sku);
        return Task.CompletedTask;
    }

    public Task<CartSummary> GetCart()
    {
        var totalAmount = _items.Sum(i => i.Quantity * i.UnitPrice);
        var cartId = this.GetPrimaryKeyString();
        var summary = new CartSummary(cartId, _items.ToList(), totalAmount);
        return Task.FromResult(summary);
    }

    public Task ClearCart()
    {
        _items.Clear();
        return Task.CompletedTask;
    }
}
