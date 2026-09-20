namespace CartPromotionEngine.Api.Models;

public record CartItem(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice
);

public record AppliedCoupon(
    string Code,
    decimal DiscountPercent,
    decimal MaxDiscountAmount,
    decimal ActualDiscount
);

public record ShoppingCart(
    Guid Id,
    Guid UserId,
    List<CartItem> Items,
    AppliedCoupon? Coupon,
    decimal SubTotal,
    decimal DiscountTotal,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record AddItemRequest(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);

public record ApplyCouponRequest(
    string CouponCode
);

public record CheckoutRequest(
    string ShippingAddress,
    string PaymentMethod
);

public record CheckoutResult(
    Guid OrderId,
    Guid CartId,
    string OrderNumber,
    decimal TotalPaid,
    int ItemsCount,
    DateTime OrderDate,
    string Status
);
