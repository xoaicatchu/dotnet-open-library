# 122 — HomeDrop

> Web app chuyển video lớn giữa điện thoại và máy tính trong gia đình, mở bằng trình duyệt, không cài ứng dụng.

**Đây là thiết kế để giao Claude Code triển khai, chưa phải ứng dụng chạy được.** Mọi luồng sản phẩm trong bộ tài liệu này dùng web app. Thiết kế native WPF/MAUI cũ đã bị thay thế.

## Người dùng thao tác thế nào?

Mở cùng một địa chỉ HomeDrop trên điện thoại và máy tính. Máy tính chọn **Nhận video** để hiện QR; điện thoại quét QR, chọn video, rồi xác nhận gửi. Hai trang web kết nối trực tiếp bằng WebRTC và truyền từng khối; máy tính hỏi nơi lưu và ghi file. Không đăng ký tài khoản, không ghép đôi lâu dài, không cài app.

~~~mermaid
sequenceDiagram
    actor P as Người dùng
    participant M as Trình duyệt điện thoại
    participant S as HomeDrop cloud<br/>trang + signaling
    participant D as Trình duyệt máy tính
    P->>D: Mở URL, bấm Nhận, hiện QR
    P->>M: Quét QR, mở URL, chọn video
    M->>S: Trao đổi tín hiệu kết nối tạm thời
    S->>D: Chuyển SDP/ICE; không nhận file
    M<-->D: WebRTC DataChannel truyền trực tiếp
    D->>P: Chọn nơi lưu / tải file
~~~

## Server là gì, video có qua đó không?

Ứng dụng cần **máy chủ dịch vụ trên Internet** để cấp trang HTTPS và giúp hai trình duyệt tìm thấy nhau (signaling). Đây là dịch vụ do đội HomeDrop triển khai/vận hành, ví dụ ASP.NET Core .NET 10 + SignalR, có domain và TLS. Nó lưu tạm mã phòng và tín hiệu trong thời gian ngắn; không lưu video.

Video đi trực tiếp giữa hai trình duyệt qua WebRTC DataChannel khi NAT/router cho phép. MVP không có TURN relay; nếu hai mạng không tạo được đường trực tiếp thì báo lỗi dễ hiểu, không âm thầm tải video lên cloud. Vì vậy không thể dùng hoàn toàn offline từ đầu; cần Internet để mở trang/bắt tay. Cùng Wi-Fi nhà cải thiện khả năng kết nối trực tiếp nhưng không bảo đảm với mọi router.

~~~mermaid
flowchart LR
    B1[Điện thoại<br/>browser] <-->|WebRTC P2P<br/>nội dung video| B2[Máy tính<br/>browser]
    B1 -->|HTTPS: SDP/ICE, room tạm| C[ASP.NET Core + SignalR<br/>web hosting + signaling]
    C -->|HTTPS: SDP/ICE| B2
    C -.->|Không nhận/lưu video trong MVP| X[(Không có kho file)]
~~~

## Giới hạn sản phẩm phải nói rõ

- Phiên bản đầu tập trung **điện thoại gửi → máy tính nhận**. Gửi nhiều GB từ browser điện thoại còn phụ thuộc browser giữ tab hoạt động và quyền đọc file.
- Desktop Chrome/Edge là đích nhận ưu tiên; nhận trên iPhone/iPad chưa cam kết vì API lưu file và quota trình duyệt không đồng đều.
- Không hứa truyền nền khi khóa màn hình/đổi ứng dụng, không tiếp tục nếu trang bị đóng mà không chọn lại nguồn.
- WebRTC có mã hóa DTLS giữa hai peer. Nếu sau này bật TURN, relay sẽ chuyển tiếp lưu lượng media và tạo chi phí băng thông; điều đó phải được công bố, giới hạn và bật theo quyết định sản phẩm.
- Resume chỉ dùng offset đã được receiver xác nhận ghi bền vững. Phải kiểm chứng resume, tải file lớn, picker và hash trên thiết bị thật trước khi quảng cáo.

## Bộ tài liệu

| Mục đích | Tài liệu |
|---|---|
| Quyết định và phạm vi | [00](docs/00-decisions-scope.md) |
| Nhu cầu và yêu cầu | [01](docs/01-needs-product.md) |
| Use case / phân rã chức năng | [02](docs/02-functions-usecases.md) |
| Kiến trúc và máy chủ | [03](docs/03-system-design.md) |
| Giao thức WebRTC / resume | [04](docs/04-transfer-protocol.md) |
| Dữ liệu và bảo mật | [05](docs/05-data-security.md) |
| Thiết kế giao diện | [06](docs/06-ui-design.md) |
| Khả thi và spike | [07](docs/07-platform-feasibility.md) |
| Kế hoạch triển khai | [08](docs/08-implementation-plan.md) |
| Nghiệm thu / phát hành | [09](docs/09-acceptance-release.md) |
| Phân tích kinh doanh | [10](docs/10-business-analysis.md) |
| Bàn giao Claude Code | [11](docs/11-claude-code-handoff.md) |
| Ý tưởng solo dev khác | [12](docs/12-solo-dev-ideas.md) |

`assets/ui-preview.html` là prototype tĩnh theo luồng web mới, chưa thực hiện signaling hoặc truyền file thật.
