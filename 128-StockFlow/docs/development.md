# Chạy nền tảng StockFlow

Yêu cầu .NET SDK 10, Node.js 22.22.1 và Docker. Frontend khóa Angular 21.2.25, CLI/build 21.2.24; Angular 22 cần Node mới hơn. Lockfile khóa dependency và npm 11.12.0 được dùng để tạo lockfile; `npm ci` hoạt động với npm 10.9.4 hiện có. Vitest dùng threads pool để tránh fork runner timeout trên Windows.

Trong PowerShell tại `128-StockFlow`, cung cấp các secret qua environment; không ghi password thật vào source hoặc shell history được chia sẻ:

```powershell
$env:STOCKFLOW_DB_PASSWORD = Read-Host 'Password PostgreSQL local'
$env:ConnectionStrings__StockFlow = "Host=localhost;Port=5438;Database=stockflow;Username=stockflow;Password=$env:STOCKFLOW_DB_PASSWORD;Maximum Pool Size=20"
docker compose up -d --wait
dotnet run --project StockFlow.Api -- --migrate
# Không bắt buộc. Bật seed development chỉ khi operator cung cấp password mạnh.
$env:DevelopmentSeed__Password = Read-Host 'Password mạnh cho các user development'
dotnet run --project StockFlow.Api
```

Trong terminal thứ hai: `cd StockFlow.Web`, `npm ci`, `npm start`. Angular tại `http://localhost:4200` proxy `/api` đến `http://localhost:5166`. Migration là lệnh riêng; API không tự migrate. Development roles là Admin, Warehouse, Viewer; tài khoản tương ứng `admin@stockflow.local`, `warehouse@stockflow.local`, `viewer@stockflow.local` chỉ được tạo khi cung cấp `DevelopmentSeed__Password` trong Development.

Production cung cấp connection string, PFX certificate Data Protection qua Secret (`DataProtection__CertificatePath`, `DataProtection__CertificatePassword` nếu PFX có password). API fail closed khi thiếu certificate. Key ring được lưu trong PostgreSQL, application name cố định `StockFlow`; mọi replica dùng cùng database và cert. Khi rotate certificate, cần giữ certificate cũ để giải mã key ring cũ theo chính sách rollout riêng.

Admin đầu tiên production được tạo bằng one-shot sau migration, dùng `Bootstrap__Email` và `Bootstrap__Password` từ Secret: `dotnet StockFlow.Api.dll --bootstrap-admin`. Lệnh yêu cầu cả hai giá trị, không ghi đè user đã tồn tại, và thoát trước khi listen HTTP. Không có demo account production.

## Kiểm tra

```powershell
dotnet restore StockFlow.slnx
dotnet build StockFlow.slnx -c Release
dotnet test StockFlow.slnx -c Release
cd StockFlow.Web
npm ci
npm test -- --watch=false
npm run build
```

Integration tests tự tạo PostgreSQL 17 container qua Testcontainers, chạy migrations trên database trống. `dotnet publish StockFlow.Api -c Release` chạy `npm ci` và build frontend rồi đóng gói bundle vào `wwwroot`; pipeline đã build frontend riêng có thể dùng `-p:SkipFrontendBuild=true` và tự copy bundle. Service Worker chỉ cache app shell/static assets, không cache `/api`. CSRF token lấy từ JSON `/api/auth/csrf`, giữ trong memory và refresh sau login/logout; cookie antiforgery là HttpOnly, interceptor chỉ gửi header cho unsafe cùng-origin `/api`.
