namespace InventoryIdServer.Api.Models;

public record InventoryItemDto(
    string Sku,
    string Name,
    int StockQuantity,
    decimal UnitPrice
);

public record TokenResponse(
    string access_token,
    string token_type,
    int expires_in
);
