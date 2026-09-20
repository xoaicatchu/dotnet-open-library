using VirtualActors.Api.Models;

namespace VirtualActors.Api.Grains;

public interface ICartGrain : IGrainWithStringKey
{
    Task AddItem(string sku, string name, int quantity, decimal unitPrice);
    Task RemoveItem(string sku);
    Task<CartSummary> GetCart();
    Task ClearCart();
}
