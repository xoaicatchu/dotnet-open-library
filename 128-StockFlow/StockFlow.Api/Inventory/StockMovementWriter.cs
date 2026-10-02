using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Common;
using StockFlow.Api.Data;

namespace StockFlow.Api.Inventory;

public sealed class StockMovementWriter(StockFlowDbContext db, TimeProvider timeProvider)
{
    private const long MaxSafeInteger = 9_007_199_254_740_991;

    public async Task WriteReceiptAsync(StockDocument document, string actorId, CancellationToken cancellationToken)
    {
        var occurredAt = timeProvider.GetUtcNow().UtcDateTime;
        var productIds = document.Lines.Select(line => line.ProductId).Distinct().ToArray();
        var activeProductIds = await db.Products.AsNoTracking().Where(product => productIds.Contains(product.Id) && product.IsActive)
            .Select(product => product.Id).ToHashSetAsync(cancellationToken);
        if (activeProductIds.Count != productIds.Length)
            throw new StockFlowOperationException(StatusCodes.Status409Conflict, "ProductInactive", "Phiếu nhập có mặt hàng đã ngừng kinh doanh.");

        foreach (var line in document.Lines.OrderBy(line => line.ProductId))
        {
            var updated = await db.StockBalances.Where(balance => balance.ProductId == line.ProductId &&
                    balance.Quantity <= MaxSafeInteger - line.Quantity && balance.Version < MaxSafeInteger)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(balance => balance.Quantity, balance => balance.Quantity + line.Quantity)
                    .SetProperty(balance => balance.Version, balance => balance.Version + 1), cancellationToken);
            if (updated != 1)
            {
                var balance = await db.StockBalances.AsNoTracking().SingleOrDefaultAsync(item => item.ProductId == line.ProductId, cancellationToken);
                if (balance is null) throw new StockFlowOperationException(StatusCodes.Status409Conflict, "BalanceMissing", "Không tìm thấy số dư tồn kho của mặt hàng.");
                if (balance.Quantity > MaxSafeInteger - line.Quantity || balance.Version >= MaxSafeInteger)
                    throw new StockFlowOperationException(StatusCodes.Status400BadRequest, "NumberOutOfRange", "Số lượng hoặc phiên bản tồn kho vượt giới hạn an toàn.");
                throw new StockFlowOperationException(StatusCodes.Status409Conflict, "BalanceConflict", "Số dư tồn kho đã thay đổi; hãy tải lại và thử lại.");
            }

            db.StockMovements.Add(new StockMovement
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                LineId = line.Id,
                ProductId = line.ProductId,
                Delta = line.Quantity,
                Kind = MovementKind.Receipt,
                OccurredAtUtc = occurredAt,
                ActorId = actorId
            });
        }
    }
}
