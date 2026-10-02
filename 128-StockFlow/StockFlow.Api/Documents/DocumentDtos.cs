namespace StockFlow.Api.Documents;

public sealed record ReceiptLineInput(Guid ProductId, long Quantity, long UnitCostVnd);

public sealed record ReceiptDraftPayload(Guid? SupplierId, IReadOnlyList<ReceiptLineInput> Lines, string? Reason = null);

public sealed record CreateDocumentRequest
{
    public Guid Id { get; init; }
    public long ExpectedVersion { get; init; }
    public string Type { get; init; } = "Receipt";
    public Guid? SupplierId { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<ReceiptLineInput> Lines { get; init; } = [];
}

public sealed record UpdateDocumentRequest
{
    public long ExpectedVersion { get; init; }
    public Guid? SupplierId { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<ReceiptLineInput> Lines { get; init; } = [];
}

public sealed record PostDocumentRequest(long ExpectedVersion);

public sealed record StockDocumentLineResponse(Guid Id, Guid ProductId, long Quantity, long? UnitCostVnd, long LineTotalVnd);

public sealed record StockDocumentResponse(
    Guid Id,
    string Number,
    string Type,
    string Status,
    Guid? SupplierId,
    string? Reason,
    DateTime CreatedAtUtc,
    DateTime? PostedAtUtc,
    string CreatedBy,
    string? PostedBy,
    long Version,
    long TotalCostVnd,
    IReadOnlyList<StockDocumentLineResponse> Lines);
