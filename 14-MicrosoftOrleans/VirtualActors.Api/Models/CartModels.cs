namespace VirtualActors.Api.Models;

[GenerateSerializer]
public record CartItem(string Sku, string Name, int Quantity, decimal UnitPrice);

[GenerateSerializer]
public record CartSummary(string CartId, List<CartItem> Items, decimal TotalAmount);
