namespace ProductCatalog.Api.Models;

public record CreateProductDto(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId
);

public record UpdateProductDto(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId
);

public record ProductResponseDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    int CategoryId,
    string? CategoryName
);

public record BatchDiscountRequest(
    decimal DiscountPercentage,
    List<int> ProductIds
);

public record BatchDiscountResult(
    int AffectedCount,
    string Message
);
