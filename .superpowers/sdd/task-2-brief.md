### Task 2 — Danh mục, nhập kho và sổ biến động

**Files:** `StockFlow.Api/Products/{ProductsController.cs,ProductDtos.cs}`, `Suppliers/{SuppliersController.cs,SupplierDtos.cs}`, `Documents/{DocumentsController.cs,DocumentDtos.cs,StockDocumentService.cs}`, `Inventory/{InventoryTransactionRunner.cs,StockMovementWriter.cs}`, `Sync/{SyncController.cs,SyncService.cs,ClientOperationStore.cs}`; `StockFlow.Tests/{CatalogTests.cs,ReceiptTests.cs,SyncIdempotencyTests.cs}`.

**Interface:** `PostAsync(Guid documentId, long expectedVersion, string actorId, CancellationToken)` trả chi tiết phiếu; `CancelAsync` cùng chữ ký. Dùng TimeProvider cho timestamp kiểm thử.

- [ ] Viết tests trước: Draft receipt 100 không tăng tồn; Post tăng 100 và một movement; gọi Post lần hai vẫn 100; sửa Posted →409; phiếu hai dòng với dòng lỗi rollback toàn bộ.
- [ ] Implement validation và CRUD danh mục/draft, chặn sửa đơn vị khi đã có movement hoặc draft đang tham chiếu; lock product row ổn định cho draft create/update/post và đổi unit để quantity không đổi đơn vị giữa lúc nhập và xác nhận. Ngừng sử dụng product bằng IsActive, không xóa hàng/nhà cung cấp đã được tham chiếu. Từ chối dòng product trùng và chặn thêm hàng inactive vào phiếu mới, vẫn cho xem lịch sử/đảo phiếu cũ.
- [ ] Trong transaction, conditional update trạng thái `WHERE Id=@id AND Status='Draft' AND Version=@version`; cập nhật balance + version, ghi journal, commit tất cả. Re-read trạng thái để phân biệt repeat thành công và stale edit; không tin trạng thái đọc trước transaction.
- [ ] PostgreSQL transient connection/serialization/deadlock retry: transaction strategy retry toàn bộ command tối đa có giới hạn và jitter/cancellation; unique idempotency key ngăn ghi lặp. Không retry validation/409. Theo dõi connection pool timeout; không retry vượt budget request.

**Acceptance:** PostgreSQL restart giữ tồn; mỗi Posted line có đúng một movement; tổng journal từng product bằng balance; giá và tổng tiền phiếu nhập chính xác, overflow trả400 và rollback. Concurrent unit update không làm thay đổi đơn vị của draft đang post; lỗi DB transient hết retry trả503 ổn định. Viewer không thể tạo product; Admin tạo product thành công và SKU trùng bị từ chối.

