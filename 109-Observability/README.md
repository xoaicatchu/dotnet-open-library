# 109-Observability

Dự án này demo cách triển khai 3 trụ cột của Observability (Metrics, Tracing, Logging) trong ứng dụng ASP.NET Core API.

## Giới thiệu 3 trụ Observability

1. **Metrics**: Đo lường dữ liệu dạng số theo thời gian (ví dụ: số request, thời gian xử lý).
2. **Distributed Tracing**: Theo dõi luồng xử lý của một request qua nhiều service.
3. **Structured Logging**: Log có cấu trúc (JSON) giúp dễ dàng tìm kiếm, lọc và phân tích (ví dụ: Serilog + Seq).

## Mermaid: Observability stack

```mermaid
flowchart TD
    App[ASP.NET Core API] -->|Logs| Serilog
    App -->|Metrics & Traces| OTel[OpenTelemetry]
    
    Serilog -->|Structured Logs| Console/Seq
    OTel -->|Traces| ConsoleExporter[Console / Jaeger]
    OTel -->|Metrics| PrometheusEndpoint[/metrics]
```

## Bảng thư viện sử dụng

| Trụ cột | Thư viện / Package | Vai trò |
| :--- | :--- | :--- |
| **Logging** | `Serilog.AspNetCore` | Structured Logging, ghi log ra Console/Seq |
| **Tracing** | `OpenTelemetry.Extensions.Hosting`<br>`OpenTelemetry.Instrumentation.AspNetCore`<br>`OpenTelemetry.Exporter.Console` | Thu thập và xuất traces (spans) ra màn hình console (hoặc Jaeger) |
| **Metrics** | `OpenTelemetry.Exporter.Prometheus.AspNetCore` | Xuất custom metrics và system metrics ra endpoint `/metrics` cho Prometheus scrape |

## Custom Metrics giải thích

`AppMetrics.cs` định nghĩa 4 custom metrics sử dụng `System.Diagnostics.Metrics`:
- `api.requests.total` (Counter): Tổng số API requests được gọi.
- `api.request.duration_ms` (Histogram): Thời gian xử lý request tính bằng ms.
- `products.created.total` (Counter): Tổng số lượng sản phẩm đã được tạo.
- `products.active.count` (UpDownCounter): Số lượng sản phẩm hiện tại trong hệ thống (tăng khi tạo, giảm khi xóa).

## Cấu trúc dự án

```
109-Observability/
├── Observability.Api/        # Project ASP.NET Core API
│   ├── Controllers/          # API Controllers
│   ├── Data/                 # Entity Framework Core DbContext
│   ├── Entities/             # Models/Entities
│   ├── Metrics/              # Custom Metrics definition
│   ├── Tracing/              # Activity Sources
│   └── Program.cs            # Setup Serilog, OpenTelemetry, EF Core, v.v.
└── Observability.Tests/      # Project xUnit Integration Tests
```

## Cách chạy

1. Mở terminal tại thư mục gốc `109-Observability`.
2. Chạy ứng dụng API:
   ```bash
   cd Observability.Api
   dotnet run
   ```
3. Truy cập Swagger UI: `http://localhost:5309/swagger`

## Endpoints

- `GET /api/products` : Lấy danh sách sản phẩm.
- `GET /api/products/{id}` : Lấy thông tin một sản phẩm theo ID.
- `POST /api/products` : Tạo mới sản phẩm.
- `DELETE /api/products/{id}` : Xóa sản phẩm.
- `GET /metrics` : Prometheus metrics endpoint.

## Kết quả test

Bạn có thể chạy toàn bộ 8/8 tests tự động trong project Tests bằng lệnh:
```bash
dotnet test
```

Mọi bài test đều pass 100%, kiểm tra tính khả dụng của API, cấu trúc log, sinh traces và Prometheus metrics exporter.
