# 11 — Prompt bàn giao Claude Code

```text
Bạn triển khai HomeDrop theo tài liệu trong thư mục 122-HomeDrop. Đọc README.md, CLAUDE.md, rồi docs/00 đến docs/09 theo thứ tự trước khi code.

Sản phẩm là web app cho người dùng phổ thông, mở trên browser và không cài đặt. Luồng v1: desktop mở trang và tạo QR nhận; điện thoại quét, chọn video; receiver xem chi tiết và chấp nhận.

Kiến trúc đã chốt: ASP.NET Core .NET 10 + SignalR trên cloud phục vụ website và signaling. Video chỉ truyền peer-to-peer qua WebRTC DataChannel. MVP không có TURN, không có file upload API, không có object storage. Nếu direct ICE fail, báo người dùng và dừng; tuyệt đối không âm thầm relay/upload. Chunks 64 KiB; bounded backpressure; receiver ACK sau write; kiểm tra final SHA-256.

Trước implementation breadth, làm M0 spike trong docs/08 và ghi bằng chứng theo docs/07, docs/09. Cụ thể chứng minh real browser connection, desktop streaming file save không buffer cả file, memory bounded, và giới hạn iOS/Android. Nếu spike thất bại, thu hẹp support matrix thay vì đổi data path.

Tạo solution .NET server + TypeScript browser client theo docs/03. Không tạo WPF, MAUI, local Kestrel hub, account, DB, Redis hoặc TURN cho MVP. Room state ephemeral: invitation TTL 10 phút; active room idle timeout 30 phút/hard limit 72 giờ. Reconnect dùng peer-scoped rotating token; reload không hứa resume. Không log room secret, SDP, ICE, filename hay bytes.

Làm từng phase trong docs/08. Viết tests cho state/protocol và browser E2E cho happy/error paths. Test implementation là phần việc được yêu cầu trong handoff này. Không đánh dấu large-file support chỉ vì build/unit tests pass; cần device evidence. Cập nhật docs nếu behavior sau spike khác dự đoán và xin quyết định trước khi đổi ADR hoặc bật relay.
```

## Tài liệu chuẩn

Nếu có mâu thuẫn, ưu tiên ADR trong docs/00; dừng triển khai và cập nhật mọi tài liệu liên quan trước khi tự thay đổi hướng media. User-facing promise và server data path phải luôn đồng bộ với code.
