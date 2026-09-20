# 106-EventSourcing-CQRS

Dự án demo Event Sourcing và CQRS trong .NET 10.

## 1. Giới thiệu Event Sourcing vs Traditional CRUD
- **Traditional CRUD**: Chỉ lưu trạng thái hiện tại (state) của thực thể vào database (UPDATE đè lên dữ liệu cũ). Mất đi lịch sử các thay đổi.
- **Event Sourcing**: Lưu mọi sự thay đổi trạng thái dưới dạng một chuỗi các sự kiện (events). Trạng thái hiện tại được xây dựng (rebuild) bằng cách chạy lại (replay) các sự kiện này.

## 2. Command flow (Write side)
`mermaid
flowchart TD
    Client-->|Command|Controller
    Controller-->|Handle|CommandHandler
    CommandHandler-->|Create/Update|AggregateRoot
    AggregateRoot-->|Raise Event|EventStore
    EventStore-->|Save|Database
`

## 3. Query flow (Read side projection)
`mermaid
flowchart TD
    EventStore-->|Publish Event|Projector
    Projector-->|Update Read Model|ReadRepository
    Client-->|Query|Controller
    Controller-->|Get|ReadRepository
`

## 4. So sánh bảng: CRUD vs Event Sourcing
| Tiêu chí | CRUD | Event Sourcing |
|---|---|---|
| Lưu trữ | Lưu trạng thái cuối | Lưu danh sách các sự kiện |
| Lịch sử | Bị ghi đè, khó theo dõi | Hoàn chỉnh, 100% audit trail |
| Hiệu năng ghi | Nhanh, trực tiếp | Chỉ append (insert), rất nhanh |
| Hiệu năng đọc | Đọc trực tiếp | Cần Projection (Read Model) để tối ưu |
| Độ phức tạp | Thấp | Cao, cần kiến trúc CQRS |

## 5. Khái niệm
- **AggregateRoot**: Thực thể chính đảm bảo tính nhất quán của dữ liệu. Nhận command và phát sinh event.
- **Event**: Hành động đã xảy ra trong quá khứ (không thể thay đổi).
- **Snapshot**: Lưu trạng thái hiện tại sau N events để tối ưu hoá việc replay (chưa dùng trong code này).
- **Projection**: Tạo Read Model từ các events để phục vụ việc query.

## 6. Cấu trúc dự án
- EventSourcing.Api: Web API với CQRS (Handlers/Commands/Queries), Domain (Aggregate/Events), ReadModel và EventStore.
- EventSourcing.Tests: Integration test kiểm chứng luồng hoạt động.

## 7. Cách chạy
`ash
dotnet build EventSourcing.slnx
dotnet run --project EventSourcing.Api
`

## 8. Endpoints
- GET /api/products: Lấy danh sách sản phẩm (Read Model)
- GET /api/products/{id}: Lấy chi tiết sản phẩm
- POST /api/products: Tạo mới sản phẩm (phát sinh ProductCreated)
- PUT /api/products/{id}: Cập nhật sản phẩm (phát sinh ProductUpdated)
- DELETE /api/products/{id}: Vô hiệu hoá sản phẩm (phát sinh ProductDeactivated)
- GET /api/events/{aggregateId}: Xem lịch sử toàn bộ event của một aggregate

## 9. Kết quả test
dotnet test EventSourcing.slnx trả về 12/12 passed tests, bao quát các case như event order, aggregate reconstruction, event trail.

## 10. Khi nào nên dùng Event Sourcing
- Cần audit trail tuyệt đối (Tài chính, Ngân hàng, Y tế).
- Hệ thống phân tán, microservices cần tính event-driven mạnh mẽ.
- Domain có các state transition phức tạp và quan trọng.
