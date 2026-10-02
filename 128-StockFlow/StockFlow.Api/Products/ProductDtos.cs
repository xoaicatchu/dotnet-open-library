namespace StockFlow.Api.Products;

public sealed record ProductRequest
{
    public string Sku { get; init; } = "";
    public string? Barcode { get; init; }
    public string Name { get; init; } = "";
    public string Unit { get; init; } = "";
    public long PurchasePriceVnd { get; init; }
    public long SalePriceVnd { get; init; }
    public long MinimumStock { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string? Barcode,
    string Name,
    string Unit,
    long PurchasePriceVnd,
    long SalePriceVnd,
    long MinimumStock,
    bool IsActive,
    long Quantity,
    long BalanceVersion);
