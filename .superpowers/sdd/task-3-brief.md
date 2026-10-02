### Task 3 — Xuất kho, hủy và bảo vệ đồng thời

**Files:** `StockFlow.Api/Documents/StockDocumentService.cs`, `StockFlow.Api/Inventory/{StockMovementWriter.cs,InventoryTransactionRunner.cs}`; `StockFlow.Tests/{IssueTests.cs,CancellationTests.cs,ConcurrentPostingTests.cs}`.

- [ ] Test receipt 100 → issue 12 → damage 2 → balance 86; xuất 87 bị409; toàn bộ phiếu nhiều dòng rollback khi một mặt hàng thiếu tồn.
- [ ] Trừ tồn bằng DB conditional update: `UPDATE StockBalances SET Quantity=Quantity-@qty, Version=Version+1 WHERE ProductId=@id AND Quantity>=@qty`; affected rows phải bằng1. Không dùng đọc tồn rồi trừ trong memory làm cơ chế duy nhất.
- [ ] Hủy tạo movement đối dấu, linked original, timestamp lúc hủy; đảo nhập chỉ thành công nếu đủ tồn. Hủy lặp lại không thêm movement; hủy phiếu xuất hoàn lại tồn; hủy Draft không ảnh hưởng kho.
- [ ] Chạy hai HTTP clients đồng thời xuất 7 từ tồn10: đúng một200, một409, tồn3. Hai clients post cùng một phiếu: hai kết quả idempotent hợp lệ, tồn chỉ biến động một lần. Test hủy nhập100 sau đã xuất12 →409, phiếu vẫn Posted.

**Transaction pseudocode chung:**

```text
retry lỗi PostgreSQL transient/deadlock trong budget giới hạn:
  begin transaction
  đọc phiếu; nếu đã ở trạng thái yêu cầu: trả existing, không thêm movement
  conditional claim trạng thái/version; sai trạng thái hoặc version: rollback,409
  kiểm tra toàn bộ dữ liệu và snapshot kiểm kê trong transaction
  mỗi dòng: conditional update balance/version; affected != 1: rollback,409
  thêm movement (hoặc reversal unique), stamp server UTC và actor
  commit; bất kỳ lỗi dòng nào: rollback toàn phiếu
```

**Acceptance:** Không tồn âm, không partial commit, không duplicate movements khi concurrent/retry; lỗi cuối cùng trả mã ổn định để UI giải thích được.

