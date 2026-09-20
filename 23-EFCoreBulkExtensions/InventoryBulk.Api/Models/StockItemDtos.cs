namespace InventoryBulk.Api.Models;

public record StockItemDto(
    int Id,
    string Sku,
    string Name,
    string Category,
    decimal Price,
    int Quantity,
    DateTime UpdatedAt
);

public record CreateStockItemDto(
    string Sku,
    string Name,
    string Category,
    decimal Price,
    int Quantity
);

public record BulkInsertRequest(
    List<CreateStockItemDto> Items
);

public record BulkUpdatePriceRequest(
    string Category,
    decimal PriceMultiplier
);

public record BulkDeleteRequest(
    string Category
);

public record BulkUpsertRequest(
    List<CreateStockItemDto> Items
);

public record BulkResultDto(
    string Operation,
    int AffectedCount,
    long ElapsedMs,
    string Message
);
