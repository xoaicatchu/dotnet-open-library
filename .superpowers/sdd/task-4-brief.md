### Task 4 — Kiểm kê và báo cáo nhập–xuất–tồn

**Files:** `StockFlow.Api/Stocktakes/{StocktakesController.cs,StocktakeDtos.cs,StocktakeService.cs}`, `Reports/{ReportsController.cs,StockReportService.cs,StockReportDtos.cs}`; `StockFlow.Tests/{StocktakeTests.cs,StockReportTests.cs}`.

- [ ] Stocktake line lưu countedQuantity và `ObservedBalanceVersion` mà người dùng nhìn thấy khi bắt đầu đếm; với đếm offline, lấy version từ cached balance tại thời điểm mở màn kiểm kê. Khi sync, server không nhận snapshot quantity làm sự thật: cùng transaction phải so version client với balance authoritative, nếu lệch trả409 và giữ nguyên local count. Nếu khớp, server mới lưu authoritative snapshot quantity/version vào draft. Khi Post, so lại snapshot version; bất kỳ dòng stale →409 và rollback toàn phiếu. Refresh online phải yêu cầu người dùng đếm/xác nhận lại từng dòng bị ảnh hưởng trước khi cập nhật snapshot.
- [ ] Adjustment = counted - snapshot; có thể âm/dương/0, không sửa phiếu Posted. Hủy kiểm kê đảo adjustment và áp dụng điều kiện tồn không âm.
- [ ] UI nhận ngày theo `Asia/Ho_Chi_Minh` rồi đổi sang UTC, API dùng khoảng `[fromUtc,toUtc)`: một câu SQL aggregate duy nhất đọc journal tại một snapshot DB cho opening=sum trước from, receipt net theo receipt + reversal của receipt, issue net theo issue + reversal của issue (hiển thị lượng xuất dương), adjustment net riêng; closing tính từ cùng các aggregate. Bút toán hủy thuộc kỳ hủy, không sửa kỳ gốc.
- [ ] Test tồn86/version7 đếm85 offline, thiết bị khác xuất2 làm version8 → sync trả409, không tạo delta +2; kiểm kê stale khi post trả409; kiểm kê đồng thời xuất không ghi đè tồn. Test report trong lúc posting phải lấy mọi aggregate từ cùng statement snapshot. Test receipt kỳ1/hủy kỳ2: kỳ1 giữ100, kỳ2 receipt net -100; boundary UTC chính xác; aggregate/net vượt miền số nguyên an toàn trả400.

**Acceptance:** Dashboard/tồn thấp dùng balance; report và lịch sử dùng journal; mọi kỳ reconciliation khớp; không trình bày tổng tiền tồn/giá vốn.

