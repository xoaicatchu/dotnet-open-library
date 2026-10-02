# 03. Kiến trúc hệ thống và lựa chọn kỹ thuật

## 1. Kiến trúc được chọn

**Modular monolith + vertical slices**, triển khai một database PostgreSQL, một API, một worker nền và một web client. Đây là lựa chọn cho đội nhỏ và project showcase: domain/workflow đủ rõ để kiểm thử độc lập, nhưng phát hành kết quả và ghi outbox vẫn nằm trong một giao dịch cục bộ.

### Các phương án đã cân nhắc

| Phương án | Ưu điểm | Chi phí/rủi ro | Kết luận |
|---|---|---|---|
| CRUD API một project, mọi logic trong controller | Dễ dựng nhanh | Trạng thái, FHIR mapping và giao dịch dễ dính chặt; khó giữ invariant và kiểm thử lỗi | Không chọn. |
| Modular monolith, nhiều project nhưng deploy chung | Ranh giới code rõ; local transaction; dễ chạy và debug | Cần thiết kế module, migration và adapter có kỷ luật | **Chọn cho M1/M2.** |
| Tách microservices theo order/specimen/result/FHIR | Có thể scale/release độc lập | Thêm distributed transactions, messaging, deployment và vận hành; chưa có nhu cầu/đội vận hành | Không chọn cho showcase. |

## 2. Sơ đồ thành phần

~~~mermaid
flowchart LR
    Staff["Collector / Technician / Reviewer / Operator"] --> Web["Angular web client"]
    Web -->|"REST + bearer token"| API["ASP.NET Core Controller API"]
    API --> App["Application feature slices"]
    App --> Domain["Domain rules and aggregates"]
    App --> Infra["Infrastructure adapters"]
    Infra --> DB[("PostgreSQL")]
    Worker[".NET Worker Service"] --> Infra
    Worker -->|"claim durable outbox"| DB
    Worker --> FHIR["FHIR integration port"]
    FHIR -->|"Demo mode: fixtures + simulator"| Simulator["Local FHIR simulator"]
    FHIR -->|"Remote mode: Firely R4 client"| EHR["Partner FHIR server"]
    API -.-> Telemetry["OpenTelemetry"]
    Worker -.-> Telemetry
~~~

*Cách đọc:* API và worker dùng chung domain, application, infrastructure và PostgreSQL. Chỉ worker gọi FHIR port để phát/gửi báo cáo; port chọn simulator local hoặc remote server theo cấu hình.

## 3. Ranh giới module

### Domain

- Value types, invariants, transition rules, domain errors and internal statuses.
- No ASP.NET, EF Core, Firely, Angular or external HTTP dependency.

### Application

- Vertical slices: ImportOrders, ListWorklist, RegisterSpecimen, ReceiveSpecimen, EnterResults, SubmitForReview, ReleaseReport, CreateCorrectionDraft, CancelCorrectionDraft, ReleaseCorrection, GetTimeline, RetryDelivery.
- Command handlers coordinate domain + interfaces. Keep authorization in API boundary/policies and repeat critical invariant checks in command handlers.
- Interfaces for repositories, current actor, clock, audit writer and FHIR exchange.

### Infrastructure

- EF Core mappings, PostgreSQL migrations, repositories, transaction boundary, durable outbox store and PublishedResourceLink persistence for stable remote FHIR identity/version metadata.
- Do not put FHIR POCO types into EF tables. Persist internal domain values and explicit import/report snapshots.

### FHIR integration

- Firely R4 types, parser/serializer, search/transaction client, structural validation, mapping and sanitized OperationOutcome conversion. Full partner IG/profile validation is enabled in remote mode with pinned packages.
- Demo and remote implementations behind one interface; no business workflow rules here.
- Remote FHIR operations use an injected, configured HttpClient/FhirClient with TLS, timeouts and explicit auth.

### API

- ASP.NET Core Controller endpoints, authentication/authorization, request validation, ProblemDetails, OpenAPI, rate limits and health checks.
- Thin controllers dispatch feature use cases. Internal API DTOs remain independent from FHIR resources.

### Worker

- Generic Host / BackgroundService processes due outbox messages and imports orders with a persisted checkpoint.
- Multiple replicas must claim work with a DB lease/locking strategy; worker shutdown releases or expires leases.
- Does not depend on an in-memory Channel for durable messages.

### Web

- Minimal Angular workflow UI: worklist, case detail, specimen receive/reject, result entry, review/release/correction, timeline and integration delivery status.
- Use a supported Angular major at implementation time, pin package-lock, and keep API contracts in a generated/checked client if that fits repo conventions.
- UI is not a security boundary; the API checks every action.

## 4. Mục tiêu source tree

~~~text
121-LabWorkflow/
├── LabWorkflow.slnx
├── README.md
├── docs/
│   ├── 01-product-requirements.md
│   ├── 02-domain-workflow.md
│   ├── 03-system-architecture.md
│   ├── 04-fhir-interoperability.md
│   ├── 05-api-ui-security.md
│   ├── 06-reliability-operations.md
│   ├── 07-testing-acceptance.md
│   ├── 08-implementation-plan.md
│   └── 09-gemini-implementation-prompt.md
├── src/
│   ├── LabWorkflow.Domain/
│   ├── LabWorkflow.Application/
│   ├── LabWorkflow.Infrastructure/
│   ├── LabWorkflow.Fhir/
│   ├── LabWorkflow.Api/
│   └── LabWorkflow.Worker/
├── web/LabWorkflow.Web/
├── tests/
│   ├── LabWorkflow.Domain.Tests/
│   ├── LabWorkflow.Application.Tests/
│   ├── LabWorkflow.Integration.Tests/
│   └── LabWorkflow.Fhir.ContractTests/
├── fixtures/fhir-r4/
├── LabWorkflow.http
└── compose.yaml
~~~

Project reference direction:

~~~text
Api -> Application
Worker -> Application
Application -> Domain
Infrastructure -> Application + Domain
Fhir -> Application + Firely SDK R4
Api/Worker -> Infrastructure + Fhir (composition root only)
Tests -> the smallest project under test
~~~

Domain and Application must not reference Infrastructure, API, Angular or Firely SDK. No circular project references.

## 5. Công nghệ và lý do

| Mối quan tâm | Công nghệ mục tiêu | Lý do |
|---|---|---|
| Runtime | .NET 10 / C# 13 (`LangVersion` 13.0 pinned) | Khớp quy ước repo; vì .NET 10 SDK mặc định C# 14 nên build phải pin rõ C# 13. |
| Web API | ASP.NET Core Controllers | Khớp quy ước root README: Controller + ApiController, không dùng Minimal API cho project này. |
| Persistence | EF Core 10 + Npgsql + PostgreSQL | Quan hệ và transaction rõ; có migration; PostgreSQL là DB dùng trong các showcase gần đây. Package provider phải tương thích EF Core 10. |
| FHIR | Firely .NET SDK 6, package Hl7.Fhir.R4; matching validation package/profile package for remote mode | Typed R4 resources, REST client, serialization and validation. M1 validates core structure; remote integration adds pinned partner profiles. Firely does not host an FHIR server. |
| Background processing | .NET Worker Service + persisted database outbox | Dễ chạy độc lập; pending work tồn tại qua restart; không cần broker cho M1. |
| Retry | Durable scheduling trong outbox; Polly/.NET HTTP resilience cho timeout/circuit ở request path | Không retry POST vô điều kiện; quy tắc retry gắn với idempotency và CapabilityStatement. |
| Authentication | ASP.NET Core OIDC/JWT bearer integration; dev-only synthetic identity provider | Không tự viết identity server; remote FHIR auth tách với nhân viên ứng dụng. |
| Web | Angular theo pattern project 120, nhưng dùng major còn được hỗ trợ khi Gemini bắt đầu | UI trực quan cho nhiều vai trò; không pin version đã cũ trong đặc tả. |
| Logs/metrics/traces | OpenTelemetry + structured logging | Theo dõi latency, lỗi và queue lag; không ghi PHI vào log. |
| Tests | xUnit, FluentAssertions, WebApplicationFactory, PostgreSQL Testcontainers, HTTP mock server | Theo convention repo; test real SQL semantics và FHIR HTTP contract mà không gọi hệ thống bên ngoài. |
| Local run | Docker Compose cho PostgreSQL; API/worker/web chạy local hoặc container | Một lệnh khởi động hạ tầng demo, vẫn dễ debug từng process. |

## 6. Cấu hình môi trường

Các cấu hình không bí mật có thể ở appsettings:

~~~text
Database: provider and connection name
Fhir:Mode = Demo | Remote
Fhir:BaseUrl
Fhir:PollIntervalSeconds
Fhir:CapabilityCheckEnabled
Fhir:ExpectedVersion = 4.0.1
Fhir:ProfilePackageId / ProfileVersion
Fhir:IdentifierSystems:Report / ResultComponent / Specimen
Fhir:BackendAuth:Mode
Security:Authority / Audience
Outbox:BatchSize / LeaseSeconds / MaximumAttempts
~~~

Secret như client private key, client credentials, connection password và encryption key lấy từ dotnet user-secrets ở Development hoặc secret manager/environment secure injection ở môi trường khác. Không commit private key/token/password vào appsettings, fixtures, .http hay logs.

## 7. Luồng ghi quan trọng

~~~mermaid
sequenceDiagram
    autonumber
    actor Reviewer
    participant Web as Angular Web
    participant API as ASP.NET Core API
    participant App as Application
    participant DB as PostgreSQL
    participant Worker as .NET Worker
    participant Adapter as FHIR Adapter
    participant FHIR as FHIR R4 Server / Simulator

    Reviewer->>Web: Xác nhận phát hành
    Web->>API: ReleaseReport(caseId, expectedVersion)
    API->>App: Kiểm tra quyền + xử lý command
    App->>DB: Transaction: ReportRevision + AuditEntry + OutboxMessage
    DB-->>App: Commit
    App-->>API: Released nội bộ; delivery Pending
    API-->>Web: Trả kết quả phát hành
    Worker->>DB: Claim outbox bằng lease
    Worker->>Adapter: Dựng bundle từ revision snapshot
    Adapter->>FHIR: Validate và gửi transaction
    alt Thành công
        FHIR-->>Adapter: Transaction-response + locations / ETags
        Adapter-->>Worker: Kết quả đã chuẩn hóa
        Worker->>DB: Lưu PublishedResourceLinks; đánh dấu Delivered
    else Lỗi tạm thời
        FHIR-->>Adapter: Timeout / 429 / 5xx
        Worker->>DB: Lên lịch retry có giới hạn
    else Lỗi cần xử lý
        FHIR-->>Adapter: 4xx / profile / version conflict
        Worker->>DB: Đánh dấu NeedsAttention
    end
~~~

*Cách đọc:* “Released” là kết quả nghiệp vụ cục bộ; “Delivered” chỉ được ghi sau phản hồi FHIR thành công hoặc đối soát. Đường API không giữ request mở trong lúc gọi server FHIR.

### Phát hành report lần đầu

1. API xác thực role Reviewer, nhận expected aggregate version.
2. Application kiểm tra invariant, specimen/result completeness và không còn source conflict.
3. Một PostgreSQL transaction ghi immutable ReportRevision, AuditEntry và OutboxMessage.
4. API trả kết quả phát hành nội bộ; không chờ EHR hoàn thành network call.
5. Worker claim outbox bằng lease, dựng FHIR bundle từ revision snapshot, validate và gửi.
6. Worker cập nhật trạng thái giao nhận và lưu sanitized response metadata.

### Correction report đã phát hành

1. LabTechnician tạo CorrectionDraft từ ReportRevision mới nhất đã Delivered/đối soát; lưu base revision, expected LabCase version, lý do, TestExecutionTime và full corrected result snapshot.
2. CorrectionDraft + audit được lưu; chưa tạo ReportRevision/outbox và không thay đổi báo cáo đã phát hành.
3. Reviewer khác author so sánh snapshot cũ/mới; release transaction tạo immutable Correction ReportRevision + audit + outbox.
4. Worker dùng PublishedResourceLink và ETag/version hiện tại cho FHIR version-aware transaction; stale remote version vào NeedsAttention/reconciliation, không bỏ If-Match.
5. Khi transaction thành công, giữ lịch sử revision nội bộ và cập nhật PublishedResourceLink từ transaction-response.

Không thể có transaction nguyên tử giữa PostgreSQL cục bộ và FHIR server bên ngoài. Outbox và idempotency giải quyết phục hồi/eventual delivery; chúng không làm mạng thành exactly-once.

## 8. Deployment profile và cổng

- Dự kiến API: HTTP 5156; Web dev server: HTTP 4300.
- PostgreSQL chỉ expose ra host cho môi trường Development.
- Worker là process riêng, dùng cùng cấu hình DB và FHIR adapter.
- Container profile production-like phải chạy non-root, dùng HTTPS reverse proxy, secrets injection, health probes và migration có kiểm soát.
- M1 compose dùng PostgreSQL + API/worker có thể chạy bằng dotnet CLI; UI chạy bằng package manager hoặc compose.

## 9. Tham khảo framework

- [.NET lifecycle](https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core)
- [EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)
- [Firely SDK 6](https://docs.fire.ly/projects/Firely-NET-SDK/en/sdk6/)
- [Npgsql EF Core provider](https://www.npgsql.org/efcore/)
- Repo convention: root [README.md](../../README.md), patterns [104-CleanVerticalSlice](../../104-CleanVerticalSlice/README.md), [114-TestingMastery](../../114-TestingMastery/README.md), [120-Realtime-TaskBoard](../../120-Realtime-TaskBoard/README.md).

