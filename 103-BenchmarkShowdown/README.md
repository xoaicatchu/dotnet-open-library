# BenchmarkShowdown

Dự án benchmark thực chiến so sánh các thư viện thường dùng trong .NET, giữa các thư viện "Classic" (Enterprise) và "Modern" (High-performance).

## Mục đích
Giúp nhóm phát triển có cái nhìn khách quan về hiệu năng (tốc độ thực thi, cấp phát bộ nhớ) trước khi quyết định áp dụng công nghệ mới.

## Các bài Benchmark

| Bài Test | Nhóm Classic | Nhóm Modern | Mục đích |
|----------|--------------|-------------|----------|
| Mapping | AutoMapper | Mapster | So sánh tốc độ và bộ nhớ khi map Entity sang DTO (Single & Bulk) |
| Validation | Manual | FluentValidation | Đánh giá chi phí khởi tạo và chạy validation |
| ORM Read | EF Core `AsNoTracking` | Dapper | So sánh tốc độ đọc dữ liệu thô (Query) |
| Caching | IMemoryCache | FusionCache | So sánh chi phí lưu/lấy dữ liệu từ bộ nhớ đệm |

## Cách chạy Benchmark

Sử dụng lệnh sau để chạy bản Release:

```bash
dotnet run --project BenchmarkShowdown.Benchmarks -c Release
```

Bạn cũng có thể lọc để chạy một bài test cụ thể:

```bash
dotnet run --project BenchmarkShowdown.Benchmarks -c Release -- --filter *MappingBenchmark*
```

## Cách chạy Smoke Tests

Smoke tests giúp đảm bảo các hàm benchmark hoạt động đúng logic (trả về cùng kết quả) trước khi được đo lường:

```bash
dotnet test
```

## Kết quả dự kiến (Mẫu)
(Placeholder)

## Kết luận
* Dùng **Classic** nếu team đã quen thuộc và không gặp vấn đề về hiệu suất.
* Cân nhắc **Modern** cho các flow cần tối ưu cao, bulk processing lớn.
