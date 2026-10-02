// AspireDemo.AppHost – Điểm trung tâm định nghĩa toàn bộ topology hệ thống.
// Chạy project này để khởi động TẤT CẢ services, databases và containers.
// Aspire Dashboard tự mở tại https://localhost:15888

using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// ── Infrastructure ─────────────────────────────────────────────────────────

// 1. Redis container (Aspire tự pull image, không cần cài Docker thủ công)
var redis = builder.AddRedis("redis")
    .WithRedisCommander(); // Thêm Redis Commander UI tại /redis-commander

// 2. PostgreSQL container
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()         // Thêm pgAdmin UI
    .AddDatabase("aspiredb");

// ── Services ───────────────────────────────────────────────────────────────

// 3. API Service
//    - Nhận connection string từ Aspire (environment variable injection tự động)
//    - Service Discovery: các service khác gọi http://apiservice thay vì hardcode port
var apiService = builder.AddProject<Projects.AspireDemo_ApiService>("apiservice")
    .WithReference(redis)         // Inject REDIS connection string
    .WithReference(postgres)      // Inject POSTGRES connection string
    .WaitFor(redis)               // Đợi Redis sẵn sàng trước khi start
    .WaitFor(postgres);           // Đợi Postgres sẵn sàng

// 4. Worker Service
//    - Subscribe vào Redis queue do ApiService publish
var worker = builder.AddProject<Projects.AspireDemo_Worker>("worker")
    .WithReference(redis)
    .WithReference(apiService)    // Có thể gọi HTTP về ApiService
    .WaitFor(apiService);

// ── Build & Run ────────────────────────────────────────────────────────────
builder.Build().Run();
