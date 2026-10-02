using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Api.Common;
using StockFlow.Api.Data;
using StockFlow.Api.Inventory;

namespace StockFlow.Api.Documents;

public sealed class StockDocumentService(
    StockFlowDbContext db,
    InventoryTransactionRunner transactions,
    StockMovementWriter movementWriter,
    TimeProvider timeProvider)
{
    private const long MaxSafeInteger = 9_007_199_254_740_991;

    public Task<StockDocumentResponse> CreateReceiptDraftAsync(Guid id, long expectedVersion, string actorId, ReceiptDraftPayload payload, CancellationToken cancellationToken) =>
        ExecuteMappedAsync(() => transactions.ExecuteAsync(token => CreateReceiptDraftWithinTransactionAsync(id, expectedVersion, actorId, payload, token), cancellationToken));

    public async Task<StockDocumentResponse> CreateReceiptDraftWithinTransactionAsync(Guid id, long expectedVersion, string actorId, ReceiptDraftPayload payload, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || expectedVersion != 0)
            throw Invalid("CreateVersion", "Phiếu nháp mới cần ID hợp lệ và expectedVersion bằng 0.");
        var validated = await ValidateReceiptAsync(payload, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var document = new StockDocument
        {
            Id = id,
            Number = $"RCV-{id:N}".ToUpperInvariant(),
            Type = DocumentType.Receipt,
            Status = DocumentStatus.Draft,
            SupplierId = payload.SupplierId,
            Reason = NormalizeReason(payload.Reason),
            CreatedAtUtc = now,
            CreatedBy = actorId,
            Version = 1,
            Lines = validated.Select(line => new StockDocumentLine
            {
                Id = Guid.NewGuid(),
                DocumentId = id,
                ProductId = line.ProductId,
                Quantity = line.Quantity,
                UnitCostVnd = line.UnitCostVnd
            }).ToList()
        };
        db.StockDocuments.Add(document);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw Conflict("DuplicateDocument", "ID hoặc số phiếu đã được sử dụng.");
        }
        return ToResponse(document);
    }

    public Task<StockDocumentResponse> UpdateReceiptDraftAsync(Guid id, long expectedVersion, string actorId, ReceiptDraftPayload payload, CancellationToken cancellationToken) =>
        ExecuteMappedAsync(() => transactions.ExecuteAsync(token => UpdateReceiptDraftWithinTransactionAsync(id, expectedVersion, actorId, payload, token), cancellationToken));

    public async Task<StockDocumentResponse> UpdateReceiptDraftWithinTransactionAsync(Guid id, long expectedVersion, string actorId, ReceiptDraftPayload payload, CancellationToken cancellationToken)
    {
        if (expectedVersion < 1 || expectedVersion >= MaxSafeInteger) throw Invalid("InvalidVersion", "expectedVersion phải là số nguyên dương trong giới hạn an toàn.");
        var document = await db.StockDocuments.AsNoTracking().Include(item => item.Lines).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null) throw new StockFlowOperationException(StatusCodes.Status404NotFound, "DocumentNotFound", "Không tìm thấy phiếu.");
        if (document.Type != DocumentType.Receipt || document.Status != DocumentStatus.Draft || document.Version != expectedVersion)
            throw Conflict("DocumentVersionConflict", "Phiếu đã đổi trạng thái hoặc phiên bản; hãy tải lại trước khi sửa.");
        var validated = await ValidateReceiptAsync(payload, cancellationToken);
        var newVersion = expectedVersion + 1;
        var changed = await db.StockDocuments.Where(item => item.Id == id && item.Type == DocumentType.Receipt && item.Status == DocumentStatus.Draft && item.Version == expectedVersion)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.SupplierId, payload.SupplierId)
                .SetProperty(item => item.Reason, NormalizeReason(payload.Reason))
                .SetProperty(item => item.Version, newVersion), cancellationToken);
        if (changed != 1)
        {
            var current = await db.StockDocuments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (current is null) throw new StockFlowOperationException(StatusCodes.Status404NotFound, "DocumentNotFound", "Không tìm thấy phiếu.");
            throw Conflict("DocumentVersionConflict", "Phiếu đã đổi trạng thái hoặc phiên bản; hãy tải lại trước khi sửa.");
        }
        await db.StockDocumentLines.Where(line => line.DocumentId == id).ExecuteDeleteAsync(cancellationToken);
        var replacementLines = validated.Select(line => new StockDocumentLine
        {
            Id = Guid.NewGuid(),
            DocumentId = id,
            ProductId = line.ProductId,
            Quantity = line.Quantity,
            UnitCostVnd = line.UnitCostVnd
        }).ToList();
        db.StockDocumentLines.AddRange(replacementLines);
        await db.SaveChangesAsync(cancellationToken);
        document.SupplierId = payload.SupplierId;
        document.Reason = NormalizeReason(payload.Reason);
        document.Version = newVersion;
        document.Lines = replacementLines;
        return ToResponse(document);
    }

    public Task<StockDocumentResponse> PostAsync(Guid documentId, long expectedVersion, string actorId, CancellationToken cancellationToken) =>
        ExecuteMappedAsync(() => transactions.ExecuteAsync(async token =>
        {
            if (expectedVersion < 1 || expectedVersion > MaxSafeInteger) throw Invalid("InvalidVersion", "expectedVersion phải là số nguyên dương trong giới hạn an toàn.");
            var document = await db.StockDocuments.AsNoTracking().Include(item => item.Lines)
                .SingleOrDefaultAsync(item => item.Id == documentId, token);
            if (document is null) throw new StockFlowOperationException(StatusCodes.Status404NotFound, "DocumentNotFound", "Không tìm thấy phiếu.");
            if (document.Type != DocumentType.Receipt) throw Conflict("UnsupportedDocumentType", "Task này chỉ xác nhận phiếu nhập.");
            if (document.Status == DocumentStatus.Posted) return ToResponse(document);
            if (document.Status != DocumentStatus.Draft) throw Conflict("InvalidDocumentState", "Chỉ phiếu nháp mới được xác nhận.");
            if (document.Version != expectedVersion) throw Conflict("DocumentVersionConflict", "Phiếu đã được sửa; hãy tải lại trước khi xác nhận.");
            _ = CalculateTotal(document.Lines.Select(line => new ReceiptLineInput(line.ProductId, line.Quantity, line.UnitCostVnd ?? -1)).ToArray());

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var claimed = await db.StockDocuments.Where(item => item.Id == documentId && item.Status == DocumentStatus.Draft && item.Version == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(item => item.Status, DocumentStatus.Posted)
                    .SetProperty(item => item.PostedAtUtc, now)
                    .SetProperty(item => item.PostedBy, actorId)
                    .SetProperty(item => item.Version, item => item.Version + 1), token);
            if (claimed != 1)
            {
                var current = await db.StockDocuments.AsNoTracking().Include(item => item.Lines).SingleOrDefaultAsync(item => item.Id == documentId, token);
                if (current?.Status == DocumentStatus.Posted) return ToResponse(current);
                if (current is null) throw new StockFlowOperationException(StatusCodes.Status404NotFound, "DocumentNotFound", "Không tìm thấy phiếu.");
                throw Conflict("DocumentVersionConflict", "Phiếu đã được sửa hoặc xác nhận bởi yêu cầu khác.");
            }

            document.Status = DocumentStatus.Posted;
            document.PostedAtUtc = now;
            document.PostedBy = actorId;
            document.Version = expectedVersion + 1;
            await movementWriter.WriteReceiptAsync(document, actorId, token);
            await db.SaveChangesAsync(token);
            return ToResponse(document);
        }, cancellationToken));

    public async Task<StockDocumentResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var document = await db.StockDocuments.AsNoTracking().Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return document is null ? null : ToResponse(document);
    }

    public async Task<(IReadOnlyList<StockDocumentResponse> Items, int TotalCount)> ListAsync(int page, int pageSize, DocumentStatus? status, CancellationToken cancellationToken)
    {
        var query = db.StockDocuments.AsNoTracking().Where(item => item.Type == DocumentType.Receipt);
        if (status is not null) query = query.Where(item => item.Status == status.Value);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderByDescending(item => item.CreatedAtUtc).ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Include(item => item.Lines).ToListAsync(cancellationToken);
        return (rows.Select(ToResponse).ToArray(), total);
    }

    private async Task<IReadOnlyList<ReceiptLineInput>> ValidateReceiptAsync(ReceiptDraftPayload payload, CancellationToken cancellationToken)
    {
        if (payload.SupplierId is null || payload.SupplierId == Guid.Empty) throw Invalid("SupplierRequired", "Phiếu nhập bắt buộc có nhà cung cấp.");
        if (payload.Reason?.Length > 500) throw Invalid("ReasonTooLong", "Ghi chú phiếu tối đa 500 ký tự.");
        if (!await db.Suppliers.AsNoTracking().AnyAsync(supplier => supplier.Id == payload.SupplierId, cancellationToken))
            throw Invalid("SupplierNotFound", "Không tìm thấy nhà cung cấp.");
        if (payload.Lines is null || payload.Lines.Count is < 1 or > 500) throw Invalid("InvalidLines", "Phiếu cần từ 1 đến 500 dòng hàng.");
        if (payload.Lines.Any(line => line.ProductId == Guid.Empty || line.Quantity is < 1 or > MaxSafeInteger || line.UnitCostVnd is < 0 or > MaxSafeInteger))
            throw Invalid("InvalidLine", "Mặt hàng, số lượng và đơn giá phải hợp lệ; số lượng phải dương.");
        if (payload.Lines.Select(line => line.ProductId).Distinct().Count() != payload.Lines.Count)
            throw Invalid("DuplicateProductLine", "Mỗi mặt hàng chỉ được xuất hiện một lần trong phiếu.");
        _ = CalculateTotal(payload.Lines);
        var ids = payload.Lines.Select(line => line.ProductId).ToArray();
        var products = await db.Products.AsNoTracking().Where(product => ids.Contains(product.Id))
            .Select(product => new { product.Id, product.IsActive }).ToDictionaryAsync(product => product.Id, cancellationToken);
        if (products.Count != ids.Length) throw Invalid("ProductNotFound", "Một hoặc nhiều mặt hàng không tồn tại.");
        if (products.Values.Any(product => !product.IsActive)) throw Conflict("ProductInactive", "Phiếu mới không được thêm mặt hàng đã ngừng kinh doanh.");
        return payload.Lines.ToArray();
    }

    private static long CalculateTotal(IReadOnlyList<ReceiptLineInput> lines)
    {
        long total = 0;
        try
        {
            foreach (var line in lines)
            {
                if (line.UnitCostVnd < 0) throw Invalid("UnitCostRequired", "Phiếu nhập cần đơn giá không âm cho mọi dòng.");
                var lineTotal = checked(line.Quantity * line.UnitCostVnd);
                if (lineTotal > MaxSafeInteger) throw Invalid("NumberOutOfRange", "Thành tiền một dòng vượt giới hạn số nguyên an toàn.");
                total = checked(total + lineTotal);
                if (total > MaxSafeInteger) throw Invalid("NumberOutOfRange", "Tổng tiền phiếu nhập vượt giới hạn số nguyên an toàn.");
            }
            return total;
        }
        catch (OverflowException)
        {
            throw Invalid("NumberOutOfRange", "Thành tiền phiếu nhập vượt giới hạn số nguyên an toàn.");
        }
    }

    private static StockDocumentResponse ToResponse(StockDocument document)
    {
        var lines = document.Lines.OrderBy(line => line.ProductId).Select(line =>
        {
            var lineTotal = checked(line.Quantity * (line.UnitCostVnd ?? 0));
            return new StockDocumentLineResponse(line.Id, line.ProductId, line.Quantity, line.UnitCostVnd, lineTotal);
        }).ToArray();
        return new StockDocumentResponse(document.Id, document.Number, document.Type.ToString(), document.Status.ToString(),
            document.SupplierId, document.Reason, document.CreatedAtUtc, document.PostedAtUtc, document.CreatedBy,
            document.PostedBy, document.Version, lines.Sum(line => line.LineTotalVnd), lines);
    }

    private static string? NormalizeReason(string? reason) => string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    private static StockFlowOperationException Invalid(string code, string message) => new(StatusCodes.Status400BadRequest, code, message);
    private static StockFlowOperationException Conflict(string code, string message) => new(StatusCodes.Status409Conflict, code, message);

    private static async Task<T> ExecuteMappedAsync<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.CheckViolation or PostgresErrorCodes.NumericValueOutOfRange)
        {
            throw Invalid("NumberOutOfRange", "Giá trị vượt giới hạn cho phép; không có thay đổi nào được lưu.");
        }
        catch (DbUpdateException exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.CheckViolation)
        {
            throw Invalid("NumberOutOfRange", "Giá trị vượt giới hạn cho phép; không có thay đổi nào được lưu.");
        }
        catch (DbUpdateException exception) when (FindPostgresException(exception)?.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw Conflict("DuplicateDocument", "ID hoặc số phiếu đã được sử dụng.");
        }
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException postgres) return postgres;
        return null;
    }
}
