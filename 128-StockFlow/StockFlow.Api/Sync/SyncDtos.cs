using System.Text.Json;
using StockFlow.Api.Documents;

namespace StockFlow.Api.Sync;

public sealed record SyncBatchRequest
{
    public IReadOnlyList<SyncOperationRequest> Operations { get; init; } = [];
}

public sealed record SyncOperationRequest
{
    public Guid ClientOperationId { get; init; }
    public string OperationType { get; init; } = "";
    public Guid ResourceId { get; init; }
    public long ExpectedVersion { get; init; }
    public JsonElement Payload { get; init; }
}

public sealed record SyncOperationResult(
    string Status,
    Guid ClientOperationId,
    Guid ResourceId,
    StockDocumentResponse? Result,
    string? Code = null,
    string? Message = null,
    int HttpStatusCode = 200);

public sealed record SyncBatchResponse(IReadOnlyList<SyncOperationResult> Results);
