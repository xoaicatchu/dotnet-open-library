namespace StockFlow.Api.Data;
public enum DocumentType { Receipt, Issue, Stocktake }
public enum DocumentStatus { Draft, Posted, Cancelled }
public enum MovementKind { Receipt, Issue, Adjustment, Reversal }
public sealed class Product
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
    public string? Barcode { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public long PurchasePriceVnd { get; set; }
    public long SalePriceVnd { get; set; }
    public long MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
}
public sealed class Supplier
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
}
public sealed class StockDocument
{
    public Guid Id { get; set; }
    public string Number { get; set; } = "";
    public DocumentType Type { get; set; }
    public DocumentStatus Status { get; set; }
    public Guid? SupplierId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PostedAtUtc { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public string CreatedBy { get; set; } = "";
    public string? PostedBy { get; set; }
    public string? CancelledBy { get; set; }
    public long Version { get; set; } = 1;
    public List<StockDocumentLine> Lines { get; set; } = [];
}
public sealed class StockDocumentLine
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid ProductId { get; set; }
    public long Quantity { get; set; }
    public long? UnitCostVnd { get; set; }
    public long? CountedQuantity { get; set; }
    public long? SnapshotQuantity { get; set; }
    public long? SnapshotBalanceVersion { get; set; }
    public long? ObservedBalanceVersion { get; set; }
}
public sealed class StockBalance
{
    public Guid ProductId { get; set; }
    public long Quantity { get; set; }
    public long Version { get; set; } = 1;
}
public sealed class StockMovement
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid LineId { get; set; }
    public Guid ProductId { get; set; }
    public long Delta { get; set; }
    public MovementKind Kind { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string ActorId { get; set; } = "";
    public Guid? ReversesMovementId { get; set; }
}
public sealed class ClientOperation
{
    public string UserId { get; set; } = "";
    public Guid ClientOperationId { get; set; }
    public string OperationType { get; set; } = "";
    public string RequestHashSha256 { get; set; } = "";
    public Guid ResourceId { get; set; }
    public int ResultStatus { get; set; }
    public string ResultJson { get; set; } = "";
    public DateTime ProcessedAtUtc { get; set; }
}
