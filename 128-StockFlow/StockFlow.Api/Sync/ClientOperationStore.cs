using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StockFlow.Api.Common;
using StockFlow.Api.Data;
using StockFlow.Api.Documents;
using StockFlow.Api.Inventory;

namespace StockFlow.Api.Sync;

public sealed class ClientOperationStore(
    StockFlowDbContext db,
    InventoryTransactionRunner transactions,
    StockDocumentService documents,
    TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Fingerprint(SyncOperationRequest operation, ReceiptDraftPayload payload)
    {
        var canonical = JsonSerializer.Serialize(new
        {
            operationType = operation.OperationType,
            resourceId = operation.ResourceId,
            expectedVersion = operation.ExpectedVersion,
            payload = new
            {
                supplierId = payload.SupplierId,
                reason = string.IsNullOrWhiteSpace(payload.Reason) ? null : payload.Reason.Trim(),
                lines = payload.Lines.OrderBy(line => line.ProductId).Select(line => new { line.ProductId, line.Quantity, line.UnitCostVnd }).ToArray()
            }
        }, JsonOptions);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    public Task<SyncOperationResult> ExecuteReceiptAsync(string userId, SyncOperationRequest operation, ReceiptDraftPayload payload, string requestHash, CancellationToken cancellationToken) =>
        transactions.ExecuteAsync(async token =>
        {
            await LockOperationAsync(userId, operation.ClientOperationId, token);
            var existing = await db.ClientOperations.AsNoTracking().SingleOrDefaultAsync(item =>
                item.UserId == userId && item.ClientOperationId == operation.ClientOperationId, token);
            if (existing is not null)
            {
                if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(existing.RequestHashSha256), Convert.FromHexString(requestHash)))
                    throw new StockFlowOperationException(StatusCodes.Status409Conflict, "IdempotencyKeyReused", "clientOperationId đã được dùng với dữ liệu khác.");
                var replay = JsonSerializer.Deserialize<SyncOperationResult>(existing.ResultJson, JsonOptions);
                if (replay is null) throw new InvalidOperationException("Stored client operation result is invalid.");
                return replay;
            }

            var document = operation.OperationType == "CreateReceipt"
                ? await documents.CreateReceiptDraftWithinTransactionAsync(operation.ResourceId, operation.ExpectedVersion, userId, payload, token)
                : await documents.UpdateReceiptDraftWithinTransactionAsync(operation.ResourceId, operation.ExpectedVersion, userId, payload, token);
            var result = new SyncOperationResult("Accepted", operation.ClientOperationId, operation.ResourceId, document);
            db.ClientOperations.Add(new ClientOperation
            {
                UserId = userId,
                ClientOperationId = operation.ClientOperationId,
                OperationType = operation.OperationType,
                RequestHashSha256 = requestHash,
                ResourceId = operation.ResourceId,
                ResultStatus = StatusCodes.Status201Created,
                ResultJson = JsonSerializer.Serialize(result, JsonOptions),
                ProcessedAtUtc = timeProvider.GetUtcNow().UtcDateTime
            });
            await db.SaveChangesAsync(token);
            return result;
        }, cancellationToken);

    private async Task LockOperationAsync(string userId, Guid operationId, CancellationToken cancellationToken)
    {
        var key = $"{userId}:{operationId:N}";
        var connection = db.Database.GetDbConnection();
        await using DbCommand command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT pg_advisory_xact_lock(hashtextextended(@operation_key, 0))";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "operation_key";
        parameter.Value = key;
        command.Parameters.Add(parameter);
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
