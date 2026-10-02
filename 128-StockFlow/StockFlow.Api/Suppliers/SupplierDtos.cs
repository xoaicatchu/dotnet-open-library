namespace StockFlow.Api.Suppliers;

public sealed record SupplierRequest
{
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Phone { get; init; }
}

public sealed record SupplierResponse(Guid Id, string Code, string Name, string? Phone);
