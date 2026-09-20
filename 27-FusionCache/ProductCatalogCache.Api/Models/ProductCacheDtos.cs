namespace ProductCatalogCache.Api.Models;

public record ProductItem(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    DateTime FetchedAt
);

public record CachedProductResponse(
    ProductItem Product,
    int TotalDbQueries
);

public record CacheStatsDto(
    int TotalDbQueries,
    bool IsDatabaseFailureSimulated
);
