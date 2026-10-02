using System.Text.Json;
using StockFlow.Api.Common;
using StockFlow.Api.Documents;

namespace StockFlow.Api.Sync;

public sealed class SyncService(ClientOperationStore operations)
{
    private const int MaxBatchSize = 50;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SyncBatchResponse> ProcessAsync(string userId, SyncBatchRequest request, CancellationToken cancellationToken)
    {
        if (request.Operations is null || request.Operations.Count is < 1 or > MaxBatchSize)
            throw Invalid("InvalidBatchSize", $"Đợt đồng bộ cần từ 1 đến {MaxBatchSize} thao tác.");

        var parsed = new List<(SyncOperationRequest Operation, ReceiptDraftPayload Payload, string Fingerprint)>(request.Operations.Count);
        foreach (var operation in request.Operations)
        {
            if (operation.ClientOperationId == Guid.Empty || operation.ResourceId == Guid.Empty)
                throw Invalid("InvalidOperation", "Thao tác cần operation ID và resource ID hợp lệ.");
            if (operation.OperationType is "CreateReceipt" && operation.ExpectedVersion != 0 ||
                operation.OperationType is "UpdateReceipt" && operation.ExpectedVersion < 1 ||
                operation.OperationType is not ("CreateReceipt" or "UpdateReceipt"))
                throw Invalid("UnsupportedOperation", "Loại thao tác đồng bộ không được hỗ trợ.");
            if (operation.Payload.ValueKind != JsonValueKind.Object)
                throw Invalid("InvalidPayload", "Payload thao tác đồng bộ phải là một đối tượng JSON.");
            ReceiptDraftPayload? payload;
            try { payload = operation.Payload.Deserialize<ReceiptDraftPayload>(JsonOptions); }
            catch (JsonException) { throw Invalid("InvalidPayload", "Payload phiếu nhập không hợp lệ."); }
            if (payload is null) throw Invalid("InvalidPayload", "Payload phiếu nhập không hợp lệ.");
            if (payload.Lines is null) throw Invalid("InvalidPayload", "Phiếu nhập cần có danh sách dòng hàng.");
            if (payload.Lines.Any(line => line is null)) throw Invalid("InvalidPayload", "Phiếu nhập chứa dòng hàng không hợp lệ.");
            parsed.Add((operation, payload, operations.Fingerprint(operation, payload)));
        }
        if (parsed.Sum(item => item.Payload.Lines.Count) > 1_000)
            throw Invalid("PayloadTooLarge", "Một đợt đồng bộ không được vượt quá 1.000 dòng hàng.");

        var results = new List<SyncOperationResult>(parsed.Count);
        foreach (var item in parsed)
        {
            try
            {
                results.Add(await operations.ExecuteReceiptAsync(userId, item.Operation, item.Payload, item.Fingerprint, cancellationToken));
            }
            catch (StockFlowOperationException exception)
            {
                results.Add(new SyncOperationResult(exception.StatusCode == StatusCodes.Status409Conflict ? "Conflict" : "Rejected",
                    item.Operation.ClientOperationId, item.Operation.ResourceId, null, exception.Code, exception.Message, exception.StatusCode));
            }
        }
        return new SyncBatchResponse(results);
    }

    private static StockFlowOperationException Invalid(string code, string message) =>
        new(StatusCodes.Status400BadRequest, code, message);
}
