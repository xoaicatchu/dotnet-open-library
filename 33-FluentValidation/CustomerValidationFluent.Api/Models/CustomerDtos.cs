namespace CustomerValidationFluent.Api.Models;

public record AddressDto(
    string Street,
    string City,
    string PostalCode,
    string Country
);

public record OrderItemDto(
    string Sku,
    string Name,
    int Quantity,
    decimal UnitPrice
);

public record CustomerRegistrationRequest(
    string FullName,
    string Email,
    int Age,
    AddressDto Address,
    bool IsVip = false,
    string? MembershipNumber = null
);

public record CreateOrderRequest(
    string CustomerEmail,
    List<OrderItemDto> Items,
    string ShippingMethod,
    decimal DiscountPercent = 0
);

public record CustomerResponse(
    int Id,
    string FullName,
    string Email,
    int Age,
    AddressDto Address,
    bool IsVip,
    string? MembershipNumber,
    DateTime CreatedAt
);

public record OrderResponse(
    string OrderId,
    string CustomerEmail,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    string ShippingMethod,
    DateTime CreatedAt
);
