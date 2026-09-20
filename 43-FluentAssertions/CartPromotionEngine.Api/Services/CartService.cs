using System.Collections.Concurrent;
using CartPromotionEngine.Api.Models;

namespace CartPromotionEngine.Api.Services;

public interface ICartService
{
    ShoppingCart GetCart(Guid cartId);
    ShoppingCart AddItem(Guid cartId, AddItemRequest request);
    ShoppingCart ApplyCoupon(Guid cartId, string couponCode);
    CheckoutResult Checkout(Guid cartId, CheckoutRequest request);
}

public class CartService : ICartService
{
    private readonly ConcurrentDictionary<Guid, ShoppingCart> _carts = new();

    private static readonly Dictionary<string, (decimal Percent, decimal MaxDiscount, decimal MinSpend)> ValidCoupons =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["SAVE10"] = (0.10m, 50.00m, 20.00m),
            ["VIP20"] = (0.20m, 100.00m, 100.00m),
            ["SUMMER2026"] = (0.15m, 75.00m, 50.00m)
        };

    public ShoppingCart GetCart(Guid cartId)
    {
        return _carts.GetOrAdd(cartId, id => new ShoppingCart(
            Id: id,
            UserId: Guid.NewGuid(),
            Items: [],
            Coupon: null,
            SubTotal: 0m,
            DiscountTotal: 0m,
            TotalAmount: 0m,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        ));
    }

    public ShoppingCart AddItem(Guid cartId, AddItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Quantity), "Quantity must be greater than zero.");
        }

        if (request.UnitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.UnitPrice), "UnitPrice cannot be negative.");
        }

        var cart = GetCart(cartId);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

        List<CartItem> updatedItems;
        if (existingItem != null)
        {
            var newQuantity = existingItem.Quantity + request.Quantity;
            var updatedItem = existingItem with
            {
                Quantity = newQuantity,
                TotalPrice = Math.Round(existingItem.UnitPrice * newQuantity, 2)
            };

            updatedItems = cart.Items
                .Where(i => i.ProductId != request.ProductId)
                .Concat([updatedItem])
                .ToList();
        }
        else
        {
            var newItem = new CartItem(
                Id: Guid.NewGuid(),
                ProductId: request.ProductId,
                ProductName: request.ProductName,
                UnitPrice: request.UnitPrice,
                Quantity: request.Quantity,
                TotalPrice: Math.Round(request.UnitPrice * request.Quantity, 2)
            );

            updatedItems = [.. cart.Items, newItem];
        }

        var subTotal = updatedItems.Sum(i => i.TotalPrice);
        var (discount, coupon) = RecalculateDiscount(subTotal, cart.Coupon?.Code);
        var totalAmount = Math.Max(0m, subTotal - discount);

        var updatedCart = cart with
        {
            Items = updatedItems,
            Coupon = coupon,
            SubTotal = subTotal,
            DiscountTotal = discount,
            TotalAmount = totalAmount,
            UpdatedAt = DateTime.UtcNow
        };

        _carts[cartId] = updatedCart;
        return updatedCart;
    }

    public ShoppingCart ApplyCoupon(Guid cartId, string couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            throw new ArgumentException("Coupon code cannot be empty.", nameof(couponCode));
        }

        if (!ValidCoupons.TryGetValue(couponCode, out var rule))
        {
            throw new ArgumentException($"Invalid or expired coupon code '{couponCode}'.", nameof(couponCode));
        }

        var cart = GetCart(cartId);
        if (cart.SubTotal < rule.MinSpend)
        {
            throw new InvalidOperationException(
                $"Coupon '{couponCode}' requires a minimum order of ${rule.MinSpend:F2}. Current subtotal is ${cart.SubTotal:F2}.");
        }

        var calculatedDiscount = Math.Min(cart.SubTotal * rule.Percent, rule.MaxDiscount);
        var actualDiscount = Math.Round(calculatedDiscount, 2);
        var appliedCoupon = new AppliedCoupon(
            Code: couponCode.ToUpperInvariant(),
            DiscountPercent: rule.Percent,
            MaxDiscountAmount: rule.MaxDiscount,
            ActualDiscount: actualDiscount
        );

        var totalAmount = Math.Max(0m, cart.SubTotal - actualDiscount);
        var updatedCart = cart with
        {
            Coupon = appliedCoupon,
            DiscountTotal = actualDiscount,
            TotalAmount = totalAmount,
            UpdatedAt = DateTime.UtcNow
        };

        _carts[cartId] = updatedCart;
        return updatedCart;
    }

    public CheckoutResult Checkout(Guid cartId, CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
        {
            throw new ArgumentException("Shipping address is required.", nameof(request.ShippingAddress));
        }

        var cart = GetCart(cartId);
        if (cart.Items.Count == 0)
        {
            throw new InvalidOperationException("Cannot checkout an empty shopping cart.");
        }

        var orderId = Guid.NewGuid();
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{orderId.ToString()[..6].ToUpper()}";
        var result = new CheckoutResult(
            OrderId: orderId,
            CartId: cart.Id,
            OrderNumber: orderNumber,
            TotalPaid: cart.TotalAmount,
            ItemsCount: cart.Items.Sum(i => i.Quantity),
            OrderDate: DateTime.UtcNow,
            Status: "Completed"
        );

        // Reset cart after checkout
        _carts[cartId] = cart with
        {
            Items = [],
            Coupon = null,
            SubTotal = 0m,
            DiscountTotal = 0m,
            TotalAmount = 0m,
            UpdatedAt = DateTime.UtcNow
        };

        return result;
    }

    private static (decimal Discount, AppliedCoupon? Coupon) RecalculateDiscount(decimal subTotal, string? couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode) || !ValidCoupons.TryGetValue(couponCode, out var rule))
        {
            return (0m, null);
        }

        if (subTotal < rule.MinSpend)
        {
            return (0m, null);
        }

        var calculatedDiscount = Math.Min(subTotal * rule.Percent, rule.MaxDiscount);
        var actualDiscount = Math.Round(calculatedDiscount, 2);
        var coupon = new AppliedCoupon(
            Code: couponCode.ToUpperInvariant(),
            DiscountPercent: rule.Percent,
            MaxDiscountAmount: rule.MaxDiscount,
            ActualDiscount: actualDiscount
        );

        return (actualDiscount, coupon);
    }
}
