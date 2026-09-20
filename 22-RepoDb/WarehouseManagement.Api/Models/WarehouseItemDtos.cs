namespace WarehouseManagement.Api.Models;

public record CreateItemDto(
    string Sku,
    string Name,
    string Location,
    int Quantity,
    decimal UnitCost
);

public record UpdateItemDto(
    string Name,
    string Location,
    int Quantity,
    decimal UnitCost
);

public record UpsertItemDto(
    string Sku,
    string Name,
    string Location,
    int Quantity,
    decimal UnitCost
);

public record ItemResponseDto(
    long Id,
    string Sku,
    string Name,
    string Location,
    int Quantity,
    decimal UnitCost,
    decimal TotalValue,
    DateTime LastRestockedAt
);

public record BatchRestockRequest(
    List<string> Skus,
    int AddedQuantity
);

public record BatchRestockResult(
    int UpdatedCount,
    string Message
);
