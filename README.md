# .NET 10 Production Library Playbook (Top 52)

Kho dự án mẫu thực hành **52 thư viện .NET hàng đầu** trên nền tảng **.NET 10 (C# 13)**. Mỗi thư mục là một giải pháp hoàn chỉnh, độc lập, sẵn sàng chạy với:
- Kiến trúc **ASP.NET Core Controller** (`[ApiController]`, kế thừa `ControllerBase`)
- Hướng dẫn tiếng Việt chi tiết theo chuẩn 12 phần
- Kiểm thử tự động độc lập (**TDD** với `xUnit` & `WebApplicationFactory`)
- Cổng HTTP riêng biệt từ `5101` đến `5152`
- File `.http` sẵn sàng test trên VS Code / Rider / Visual Studio

---

## Danh mục 52 Dự Án

### Batch 1: Projects 01–10 (Web API, Messaging & Distributed Systems)
| # | Dự án | Thư viện | Tình huống thực tế | Port | Tests |
|---|---|---|---|:---:|:---:|
| 01 | [01-FastEndpoints](01-FastEndpoints/README.md) | FastEndpoints | REPR Pattern, Clean Architecture API sản phẩm | 5101 | 17/17 |
| 02 | [02-Carter](02-Carter/README.md) | Carter | Thin controller & CarterModule quản lý danh mục | 5102 | 7/7 |
| 03 | [03-Wolverine](03-Wolverine/README.md) | Wolverine | Mediator & Message Bus không cần interface | 5103 | 6/6 |
| 04 | [04-MediatR](04-MediatR/README.md) | MediatR | CQRS & Pipeline Behaviors (Validation, Logging) | 5104 | 6/6 |
| 05 | [05-Scrutor](05-Scrutor/README.md) | Scrutor | Assembly Scanning & Decorator Pattern DI | 5105 | 9/9 |
| 06 | [06-MassTransit](06-MassTransit/README.md) | MassTransit | Message Broker (In-Memory / RabbitMQ) & Consumer | 5106 | 9/9 |
| 07 | [07-CAP](07-CAP/README.md) | CAP | Transactional Outbox & Event-Driven Architecture | 5107 | 6/6 |
| 08 | [08-Rebus](08-Rebus/README.md) | Rebus | Lean Service Bus & Sagas điều phối giao dịch | 5108 | 9/9 |
| 09 | [09-NServiceBus](09-NServiceBus/README.md) | NServiceBus | Enterprise Messaging & Message Routing | 5109 | 7/7 |
| 10 | [10-Silverback](10-Silverback/README.md) | Silverback | Kafka / Broker abstraction & Inbound/Outbound | 5110 | 9/9 |

### Batch 2: Projects 11–20 (Streaming, Real-time, Actors & Background Jobs)
| # | Dự án | Thư viện | Tình huống thực tế | Port | Tests |
|---|---|---|---|:---:|:---:|
| 11 | [11-ConfluentKafka](11-ConfluentKafka/README.md) | Confluent.Kafka | High-throughput Kafka Producer & Consumer | 5111 | 8/8 |
| 12 | [12-MQTTnet](12-MQTTnet/README.md) | MQTTnet | Embedded MQTT Broker & IoT Device Simulator | 5112 | 5/5 |
| 13 | [13-SignalR](13-SignalR/README.md) | SignalR | Real-time WebSocket streaming & Chat rooms | 5113 | 5/5 |
| 14 | [14-MicrosoftOrleans](14-MicrosoftOrleans/README.md) | Microsoft Orleans | Virtual Actor Model (Grains, State, Placement) | 5114 | 6/6 |
| 15 | [15-AkkaNET](15-AkkaNET/README.md) | Akka.NET | Actor System, Mailbox & State Machine | 5115 | 10/10 |
| 16 | [16-Hangfire](16-Hangfire/README.md) | Hangfire | Background Job Processing, Cron & Dashboard | 5116 | 5/5 |
| 17 | [17-QuartzNET](17-QuartzNET/README.md) | Quartz.NET | Enterprise Job Scheduling & Trigger listeners | 5117 | 4/4 |
| 18 | [18-Coravel](18-Coravel/README.md) | Coravel | Lightweight In-process Scheduler, Queue & Event | 5118 | 5/5 |
| 19 | [19-EFCore](19-EFCore/README.md) | Entity Framework Core | Relational ORM, Change Tracker & AsNoTracking | 5119 | 10/10 |
| 20 | [20-Dapper](20-Dapper/README.md) | Dapper | High-performance Micro-ORM & Multi-mapping | 5120 | 13/13 |

### Batch 3: Projects 21–30 (Data Access, Caching & Resilience)
| # | Dự án | Thư viện | Tình huống thực tế | Port | Tests |
|---|---|---|---|:---:|:---:|
| 21 | [21-LinqToDB](21-LinqToDB/README.md) | LinqToDB | LINQ-to-SQL siêu tốc, BulkCopy & Set-based Update | 5121 | 13/13 |
| 22 | [22-RepoDb](22-RepoDb/README.md) | RepoDb | Hybrid ORM tốc độ cao, Property Handlers | 5122 | 15/15 |
| 23 | [23-EFCoreBulkExtensions](23-EFCoreBulkExtensions/README.md) | EF Core Native Batch | ExecuteUpdateAsync, ExecuteDeleteAsync & Batching | 5123 | 9/9 |
| 24 | [24-Marten](24-Marten/README.md) | Marten | PostgreSQL JSONB Document DB & Event Sourcing | 5124 | 10/10 |
| 25 | [25-FluentMigrator](25-FluentMigrator/README.md) | FluentMigrator | C# Fluent Database Schema Migrations & Rollback | 5125 | 2/2 suites |
| 26 | [26-DbUp](26-DbUp/README.md) | DbUp | Raw SQL Script Migrations (`.sql`) & Journaling | 5126 | 1/1 suite |
| 27 | [27-FusionCache](27-FusionCache/README.md) | FusionCache | Anti-Stampede Lock, Fail-Safe & L1 Memory Cache | 5127 | 2/2 suites |
| 28 | [28-EasyCaching](28-EasyCaching/README.md) | EasyCaching | Caching Abstraction, Multi-provider, Prefix Query | 5128 | 5/5 |
| 29 | [29-StackExchangeRedis](29-StackExchangeRedis/README.md) | StackExchange.Redis | Redis Data Structures (Hash, Set, ZSet) & Pub/Sub | 5129 | 7/7 |
| 30 | [30-Polly](30-Polly/README.md) | Polly v8 (Polly.Core) | Resilience Pipeline (Retry, Circuit Breaker, Fallback) | 5130 | 6/6 |

### Batch 4: Projects 31–40 (Mapping, Validation, HTTP, Auth & Observability)
| # | Dự án | Thư viện | Tình huống thực tế | Port | Tests |
|---|---|---|---|:---:|:---:|
| 31 | [31-AutoMapper](31-AutoMapper/README.md) | AutoMapper | Object Mapping, Flattening, Projection, Profiles | 5131 | 7/7 |
| 32 | [32-Mapster](32-Mapster/README.md) | Mapster | High Performance Object Mapping, TypeAdapterConfig | 5132 | 6/6 |
| 33 | [33-FluentValidation](33-FluentValidation/README.md) | FluentValidation | Strongly-typed Validation Rules, Child Validators | 5133 | 12/12 |
| 34 | [34-Refit](34-Refit/README.md) | Refit | Type-safe REST Client, Declarative HTTP Interface | 5134 | 10/10 |
| 35 | [35-Flurl](35-Flurl/README.md) | Flurl.Http | Fluent URL Builder & HTTP Client, HttpTest | 5135 | 8/8 |
| 36 | [36-OpenIddict](36-OpenIddict/README.md) | OpenIddict | OAuth 2.0 / OIDC Server, Client Credentials Flow | 5136 | 6/6 |
| 37 | [37-DuendeIdentityServer](37-DuendeIdentityServer/README.md) | Duende IdentityServer | Enterprise Identity & Access Management, Local API | 5137 | 6/6 |
| 38 | [38-Serilog](38-Serilog/README.md) | Serilog | Structured Logging, LogContext, Sinks | 5138 | 5/5 |
| 39 | [39-NLog](39-NLog/README.md) | NLog | Flexible Logging, XML Config, MemoryTarget | 5139 | 5/5 |
| 40 | [40-OpenTelemetry](40-OpenTelemetry/README.md) | OpenTelemetry | Distributed Tracing (ActivitySource), Metrics (Meter) | 5140 | 5/5 |

### Batch 5: Projects 41–47 (Testing, Documentation & Utilities)
| # | Dự án | Thư viện | Tình huống thực tế | Port | Tests |
|---|---|---|---|:---:|:---:|
| 41 | [41-Bogus](41-Bogus/README.md) | Bogus | Fake data generator, Deterministic seeding, Locales | 5141 | 12/12 |
| 42 | [42-Verify](42-Verify/README.md) | Verify (Verify.Xunit) | Snapshot testing, Scrubbing Guids/Dates, Invoices | 5142 | 7/7 |
| 43 | [43-FluentAssertions](43-FluentAssertions/README.md) | FluentAssertions | Natural assertions, Deep equivalency, Shopping Cart | 5143 | 13/13 |
| 44 | [44-BenchmarkDotNet](44-BenchmarkDotNet/README.md) | BenchmarkDotNet | Nanosecond benchmarking, MemoryDiagnoser, JIT warmup | 5144 | 8/8 |
| 45 | [45-SpecFlow](45-SpecFlow/README.md) | SpecFlow (SpecFlow.xUnit) | BDD Gherkin scenarios, Living Documentation, Banking | 5145 | 11/11 |
| 46 | [46-Swashbuckle](46-Swashbuckle/README.md) | Swashbuckle.AspNetCore | OpenAPI 3.0, Multi-version (V1/V2), Operation Filters | 5146 | 9/9 |
| 47 | [47-NSwag](47-NSwag/README.md) | NSwag | OpenAPI toolchain, Swagger UI, ReDoc, C#/TS SDK gen | 5147 | 11/11 |

### Batch 6: Projects 48–52 (Files, Parsing & Workflow)
| # | Dự án | Thư viện | Tình huống thực tế | Port | Tests |
|---|---|---|---|:---:|:---:|
| 48 | [48-Elsa](48-Elsa/README.md) | Elsa Workflows | Code-first Workflow engine, In-process runner, Order approval | 5148 | 8/8 |
| 49 | [49-Stateless](49-Stateless/README.md) | Stateless | Finite State Machine, Transition guards, History, Mermaid/DOT | 5149 | 8/8 |
| 50 | [50-QuestPDF](50-QuestPDF/README.md) | QuestPDF | Code-first PDF generation, Fluent layout, Invoice documents | 5150 | 6/6 |
| 51 | [51-ClosedXML](51-ClosedXML/README.md) | ClosedXML | Excel reader & writer (.xlsx), Styled reports, Formula, Roundtrip | 5151 | 6/6 |
| 52 | [52-CsvHelper](52-CsvHelper/README.md) | CsvHelper | High-performance CSV import/export, ClassMap, Streaming | 5152 | 7/7 |

### Dự án Tổng Hợp: 100-AllInOne (Toàn bộ 52 Thư Viện trong 1 Use Case)
| # | Dự án | Thư viện | Tình huống thực tế | Port | Tests |
|---|---|---|---|:---:|:---:|
| 100 | [100-AllInOne](100-AllInOne/README.md) | 52 Thư Viện Toàn Diện | E-Commerce Order Fulfillment & Audit Platform | 5200 | 10/10 |

---

## Chi tiết Năng lực & Usecase Thực tế của Từng Thư viện

### Batch 1: Web API, Messaging & Distributed Systems (01–10)

#### 01. FastEndpoints
- **Năng lực cốt lõi**: Triển khai kiến trúc REPR (Request-Endpoint-Response), tự động phát hiện endpoint qua assembly scanning, tích hợp sẵn FluentValidation pipeline, hỗ trợ response caching và security policies trực tiếp trên endpoint.
- **Usecase thực tế**:
  - Xây dựng hệ thống Microservices quy mô lớn theo phong cách Vertical Slice Architecture (mỗi feature nằm trọn vẹn trong một thư mục).
  - Thay thế ASP.NET Core Controller truyền thống để loại bỏ overhead, tăng throughput (RPS) và ngăn chặn tình trạng Controller phình to (God Controller).
  - Xây dựng API kiểm tra dữ liệu nghiêm ngặt ngay tại cửa ngõ trước khi vào tầng logic nghiệp vụ.

#### 02. Carter
- **Năng lực cốt lõi**: Đóng gói các route Minimal API vào các class `ICarterModule` độc lập, tự động đăng ký vào DI và routing table, cung cấp Fluent Validation extension và Content Negotiation.
- **Usecase thực tế**:
  - Tổ chức và module hóa hàng chục hoặc hàng trăm endpoint Minimal APIs trong dự án lớn mà không làm bừa bãi file `Program.cs`.
  - Xây dựng các plugin hoặc tính năng có thể bật/tắt động bằng cách nạp/hủy nạp assembly.
  - Phù hợp cho các Web API ưu tiên tốc độ khởi động nhanh và dung lượng bộ nhớ thấp (Container / Serverless).

#### 03. Wolverine
- **Năng lực cốt lõi**:
  - **In-process Command Mediator**: Thay thế MediatR với cú pháp Zero-Interface (không bắt buộc kế thừa `IRequest` hay `IRequestHandler`), code thuần POCO.
  - **Asynchronous Messaging**: Gửi/nhận message qua broker ngoài (RabbitMQ, Kafka, Azure Service Bus, AWS SQS) với cùng một mô hình handler.
  - **Transactional Outbox & Inbox**: Tự động lưu message vào outbox table cùng transaction với database, cam kết không mất mát message.
  - **Cascading Messages & Side Effects**: Handler có thể trả về một tuple chứa nhiều message hoặc event con để Wolverine tự động phân phối tiếp mà không cần inject `IMessageBus`.
  - **Compile-time Code Generation**: Sử dụng Lamar/Jasper để sinh mã C# trước khi chạy, đạt tốc độ gần như gọi hàm trực tiếp.
- **Usecase thực tế**:
  - Triển khai CQRS (Command Query Responsibility Segregation) và Event-Driven Architecture chuẩn mực trong hệ thống phân tán.
  - Xử lý các nghiệp vụ phức tạp có chuỗi phản ứng nhiều bước (ví dụ: Tạo đơn hàng -> Bắn event trừ kho -> Bắn event gửi email -> Bắn event tích điểm).
  - Tích hợp liền mạch giữa Marten (Event Sourcing) và PostgreSQL hoặc EF Core và SQL Server.

#### 04. MediatR
- **Năng lực cốt lõi**: Trừu tượng hóa giao tiếp in-process giữa các thành phần thông qua Requests (1-1) và Notifications (1-N), hỗ trợ mạnh mẽ Pipeline Behaviors (middleware in-process).
- **Usecase thực tế**:
  - Triển khai Clean Architecture / Onion Architecture: Tách rời Controller (tầng Web) khỏi Handler (tầng Application).
  - Xử lý tập trung các Cross-cutting Concerns: Tự động ghi log, kiểm tra validation, đo thời gian thực thi, quản lý transaction cho mọi request qua Pipeline Behaviors.
  - Phát sự kiện nội bộ (`INotification`) khi có thay đổi trạng thái để các module khác lắng nghe trong cùng một tiến trình.

#### 05. Scrutor
- **Năng lực cốt lõi**: Mở rộng `Microsoft.Extensions.DependencyInjection` với khả năng quét assembly theo quy ước (Convention-based registration) và triển khai Decorator Pattern tự nhiên cho DI.
- **Usecase thực tế**:
  - Tự động đăng ký toàn bộ Service và Repository theo interface tương ứng (`I...Service` -> `...Service`) mà không phải viết hàng trăm dòng `AddScoped`.
  - Triển khai Decorator Pattern: Bọc thêm tính năng logging, caching hoặc retry xung quanh một service gốc mà không làm sửa đổi mã nguồn service đó.
  - Quét và nạp các dynamic module trong kiến trúc Modular Monolith.

#### 06. MassTransit
- **Năng lực cốt lõi**: Enterprise Service Bus mã nguồn mở hoàn chỉnh, hỗ trợ RabbitMQ, Azure Service Bus, Amazon SQS, Kafka; tích hợp sẵn Saga State Machine, Outbox Pattern, Dead Letter Queue và Consumer Fault Handling.
- **Usecase thực tế**:
  - Giao tiếp bất đồng bộ, tin cậy giữa các microservices trong hệ sinh thái thương mại điện tử, thanh toán, vận chuyển.
  - Điều phối quy trình phân tán dài hạn (Distributed Sagas) như đặt vé máy bay, phòng khách sạn, hoàn tiền khi một bước thất bại.
  - Xử lý hàng đợi chịu tải cao với cơ chế Auto-Retry, Exponential Backoff và Circuit Breaker tích hợp sẵn.

#### 07. CAP
- **Năng lực cốt lõi**: Triển khai Transactional Outbox Pattern kết hợp EventBus cho kiến trúc Microservices, hỗ trợ đa dạng Storage (SQL Server, MySQL, PostgreSQL, MongoDB) và Broker (RabbitMQ, Kafka, Redis Streams, Azure Service Bus).
- **Usecase thực tế**:
  - Đảm bảo tính nhất quán dữ liệu tuyệt đối (Eventual Consistency) giữa thao tác ghi vào Database nghiệp vụ và thao tác xuất bản Message ra Broker.
  - Lọc trùng lặp tự động (Idempotent Consumer) ở phía nhận dữ liệu, loại bỏ nguy cơ xử lý trùng giao dịch thanh toán hoặc đơn hàng.
  - Hệ thống Tài chính, Ngân hàng, Cổng thanh toán yêu cầu mức độ tin cậy giao dịch 100%.

#### 08. Rebus
- **Năng lực cốt lõi**: Thư viện Service Bus tinh gọn (lean and modern), dễ cấu hình, hỗ trợ định tuyến 1-1 (Send) và 1-N (Publish), tích hợp quản lý Saga và đa dạng transport.
- **Usecase thực tế**:
  - Thay thế các giải pháp Service Bus cồng kềnh cho các ứng dụng vừa và nhỏ cần giao tiếp bất đồng bộ qua hàng đợi.
  - Quản lý trạng thái quy trình nghiệp vụ nhiều bước (Saga) với cú pháp C# đơn giản.
  - Thích hợp cho các team muốn nắm rõ toàn bộ luồng hoạt động của message bus mà không bị che giấu bởi quá nhiều tầng trừu tượng.

#### 09. NServiceBus
- **Năng lực cốt lõi**: Nền tảng nhắn tin phân tán cấp doanh nghiệp (Enterprise-grade), cung cấp bộ công cụ giám sát trực quan (ServicePulse, ServiceInsight), quản lý Outbox, Saga, tự động phục hồi lỗi cấp cao.
- **Usecase thực tế**:
  - Hệ thống tài chính ngân hàng, bảo hiểm, chăm sóc sức khỏe có yêu cầu SLA khắt khe và ràng buộc pháp lý về kiểm toán dữ liệu.
  - Giám sát luồng tin nhắn và phân tích sự cố phân tán trực quan bằng giao diện đồ họa.
  - Xử lý các quy trình nghiệp vụ có thời gian chờ kéo dài nhiều tuần hoặc tháng.

#### 10. Silverback
- **Năng lực cốt lõi**: Khung làm việc chuyên biệt cho Apache Kafka và RabbitMQ, cung cấp trừu tượng hóa mức cao, hỗ trợ Transactional Outbox/Inbox, Batch Processing, Partition Rebalancing và Schema Registry.
- **Usecase thực tế**:
  - Xử lý luồng sự kiện tốc độ cao (Event Streaming) với Kafka trong các hệ thống Big Data, IoT hoặc viễn thông.
  - Đảm bảo ngữ nghĩa phân phối Exactly-Once hoặc At-Least-Once trong môi trường container động.
  - Đồng bộ dữ liệu bất đồng bộ giữa các cơ sở dữ liệu phân tán (Data Replication).

---

### Batch 2: Streaming, Real-time, Actors & Background Jobs (11–20)

#### 11. Confluent.Kafka
- **Năng lực cốt lõi**: Thư viện client Apache Kafka chính thức từ Confluent, được tối ưu hóa bằng C-driver (`librdkafka`), mang lại hiệu năng throughput cao nhất và độ trễ thấp nhất.
- **Usecase thực tế**:
  - Thu thập hàng triệu bản ghi telemetry, log, metrics hoặc clickstream mỗi giây.
  - Giao tiếp giữa các microservices yêu cầu thông lượng cực lớn mà RabbitMQ không đáp ứng đủ.
  - Triển khai kiến trúc Event Sourcing quy mô lớn.

#### 12. MQTTnet
- **Năng lực cốt lõi**: Thư viện MQTT hiệu năng cao cho .NET, hỗ trợ đầy đủ MQTT v3.1.1 và v5.0, cho phép tạo cả MQTT Client và nhúng trực tiếp MQTT Server/Broker vào ứng dụng.
- **Usecase thực tế**:
  - Kết nối và thu thập dữ liệu từ hàng ngàn thiết bị IoT, cảm biến công nghiệp, đồng hồ thông minh với băng thông siêu thấp.
  - Tự xây dựng MQTT Broker nội bộ cho nhà máy hoặc tòa nhà thông minh mà không cần cài thêm Mosquitto hay HiveMQ.
  - Đẩy lệnh điều khiển thời gian thực xuống các thiết bị phần cứng nhúng (ESP32, Raspberry Pi).

#### 13. SignalR
- **Năng lực cốt lõi**: Thư viện giao tiếp hai chiều thời gian thực (Real-time Full-Duplex) giữa Client và Server qua WebSocket, Server-Sent Events (SSE) hoặc Long Polling với cơ chế fallback tự động.
- **Usecase thực tế**:
  - Bảng điều khiển (Dashboard) trực tiếp: biểu đồ chứng khoán, kết quả bóng đá, số lượng truy cập thời gian thực.
  - Ứng dụng chat, nhắn tin nhóm, làm việc cộng tác đa người dùng (Google Docs clone).
  - Thông báo đẩy (Push Notifications) trong ứng dụng web/mobile khi có đơn hàng mới hoặc cảnh báo hệ thống.

#### 14. Microsoft Orleans
- **Năng lực cốt lõi**: Framework Virtual Actor phân tán của Microsoft, tự động quản lý vòng đời Grain (kích hoạt, nạp RAM, lưu trữ, thu hồi) mà lập trình viên không phải lo về concurrency hay distributed locks.
- **Usecase thực tế**:
  - Quản lý giỏ hàng thương mại điện tử và phiên đăng nhập: Mỗi giỏ hàng là một Actor chạy độc lập trên cụm server, xử lý đồng thời cực cao không bị xung đột.
  - Game server trực tuyến: Quản lý trạng thái hàng triệu người chơi và phòng đấu trong không gian ảo.
  - Digital Twins & Quản lý thiết bị IoT: Mỗi thiết bị vật lý có một bản sao Grain số hóa trên đám mây.

#### 15. Akka.NET
- **Năng lực cốt lõi**: Cổng .NET của Apache Pekko/Akka (JVM), triển khai mô hình Actor thuần túy, giám sát cây phân cấp (Supervision Hierarchies), xử lý thông điệp qua Mailbox đơn luồng an toàn.
- **Usecase thực tế**:
  - Ứng dụng giao dịch tài chính tốc độ cao, sàn giao dịch tiền mã hóa cần xử lý lệnh mua/bán với độ trễ microsecond.
  - Hệ thống yêu cầu khả năng chịu lỗi tối đa ("Let it crash"): tự động khôi phục Actor con khi xảy ra ngoại lệ.
  - Mô phỏng thực tế phức tạp (traffic simulation, mô hình vi mô).

#### 16. Hangfire
- **Năng lực cốt lõi**: Thư viện xử lý tác vụ nền (Background Jobs) toàn diện, lưu trữ job bền vững (Persistent Storage), hỗ trợ Fire-and-Forget, Delayed, Recurring (Cron) và Continuations kèm Dashboard Web tích hợp.
- **Usecase thực tế**:
  - Xuất báo cáo Excel/PDF dung lượng lớn và gửi email đính kèm sau khi người dùng yêu cầu.
  - Lập lịch tự động: Đồng bộ dữ liệu kho hàng vào 01:00 AM mỗi ngày, dọn dẹp file tạm hàng tuần.
  - Tự động thử lại khi gọi API bên thứ ba thất bại với cơ chế backoff.

#### 17. Quartz.NET
- **Năng lực cốt lõi**: Hệ thống lập lịch tác vụ doanh nghiệp lâu đời và hoàn chỉnh nhất, hỗ trợ biểu thức Cron cực kỳ phức tạp (lịch ngày lễ, tuần làm việc), cơ chế Misfire Instruction và Clustered Execution.
- **Usecase thực tế**:
  - Lập lịch thanh toán lương nhân viên, tính lãi suất ngân hàng vào ngày làm việc cuối cùng của tháng.
  - Chạy các tác vụ batch quy mô lớn trên cụm nhiều server (Cluster) với cơ chế khóa hàng qua database, cam kết không chạy trùng lặp.
  - Tự động bù đắp các lượt chạy bị bỏ lỡ (Misfire) khi server bị khởi động lại.

#### 18. Coravel
- **Năng lực cốt lõi**: Thư viện siêu nhẹ cung cấp Scheduler, Queue, Caching, Event và Mailer in-process theo phong cách Laravel, không yêu cầu cơ sở dữ liệu lưu trữ cấu hình.
- **Usecase thực tế**:
  - Ứng dụng Monolith vừa và nhỏ cần lập lịch định kỳ bằng cú pháp C# Fluent tự nhiên (`scheduler.Schedule<MyJob>().DailyAt(13, 30)`).
  - Đưa các tác vụ nặng vào hàng đợi in-memory (`IQueue`) để trả response về cho client ngay lập tức.
  - Dự án không muốn cài đặt hay bảo trì cơ sở dữ liệu riêng cho background jobs như Hangfire.

#### 19. Entity Framework Core (EF Core)
- **Năng lực cốt lõi**: Trình ánh xạ quan hệ - đối tượng (ORM) đầy đủ tính năng chính thức của Microsoft, hỗ trợ LINQ to SQL, Change Tracking, Eager/Lazy/Explicit Loading, Migrations và Concurrency Control.
- **Usecase thực tế**:
  - Ứng dụng nghiệp vụ doanh nghiệp (Line of Business) với hàng trăm bảng dữ liệu có quan hệ phức tạp (1-1, 1-N, N-N).
  - Quản lý phiên bản cấu trúc cơ sở dữ liệu tự động theo mã nguồn qua EF Core Migrations.
  - Tối ưu hóa truy vấn chỉ đọc bằng `.AsNoTracking()` và projection dữ liệu trực tiếp sang DTO bằng `.Select()`.

#### 20. Dapper
- **Năng lực cốt lõi**: Micro-ORM siêu nhanh được phát triển bởi Stack Overflow, mở rộng `IDbConnection` với cơ chế Dynamic IL Deserialization, mapping trực tiếp kết quả SQL sang C# object với tốc độ tiệm cận ADO.NET thuần.
- **Usecase thực tế**:
  - Các truy vấn đọc dữ liệu phức tạp đòi hỏi tối ưu hóa câu lệnh SQL thủ công (CTE, Window Functions, Stored Procedures).
  - Phía Đọc (Read-side) trong kiến trúc CQRS: EF Core nhận Command ghi, Dapper xử lý Query đọc.
  - Các ứng dụng web có lưu lượng truy cập khổng lồ cần giảm thiểu CPU và RAM cấp phát khi truy vấn CSDL.

---

### Batch 3: Data Access, Caching & Resilience (21–30)

#### 21. LinqToDB
- **Năng lực cốt lõi**: Trình ORM hướng LINQ siêu tốc, sinh câu lệnh SQL trong suốt, hỗ trợ BulkCopy nguyên bản, cập nhật/xóa theo tập hợp (Set-based operations: `Update`, `Delete`) và các câu lệnh đặc thù (CTE, MERGE, Table Hints).
- **Usecase thực tế**:
  - Chèn hàng triệu bản ghi vào cơ sở dữ liệu trong vài giây qua `BulkCopyAsync` mà không cần công cụ bên ngoài.
  - Thực hiện các lệnh cập nhật hàng loạt trực tiếp trên CSDL (`db.Orders.Where(...).Update(x => ...)`) mà không cần nạp thực thể vào bộ nhớ.
  - Phù hợp cho các hệ thống Data Warehouse, xử lý dữ liệu lớn (ETL).

#### 22. RepoDb
- **Năng lực cốt lõi**: Hybrid ORM cân bằng hoàn hảo giữa tốc độ của Dapper và tính tiện dụng của EF Core, cung cấp sẵn các thao tác CRUD (`Insert`, `Update`, `Merge`, `Delete`) không cần viết SQL, kết hợp bộ nhớ đệm metadata tự động.
- **Usecase thực tế**:
  - Phát triển API CRUD siêu tốc mà không phải viết câu lệnh SQL thủ công như Dapper, cũng không chịu overhead Change Tracker của EF Core.
  - Đồng bộ hóa dữ liệu thông qua thao tác `Merge` (Upsert: chèn nếu chưa có, cập nhật nếu đã tồn tại).
  - Thao tác dữ liệu hàng loạt với `InsertAll`, `UpdateAll` tối ưu cao.

#### 23. EF Core Native Batch (EFCoreBulkExtensions)
- **Năng lực cốt lõi**: Tận dụng tính năng Native Batching của EF Core 7/8/9/10 (`ExecuteUpdateAsync`, `ExecuteDeleteAsync`) và thư viện mở rộng để thực thi các thao tác hàng loạt trực tiếp tại CSDL.
- **Usecase thực tế**:
  - Cập nhật trạng thái hàng ngàn đơn hàng hết hạn chỉ bằng một câu lệnh SQL duy nhất mà không cần tải dữ liệu vào RAM.
  - Xóa hàng loạt dữ liệu log hoặc giỏ hàng cũ định kỳ mà không bị lỗi tràn bộ nhớ.
  - Kết hợp với cấu hình Fluent API có sẵn của `DbContext`.

#### 24. Marten
- **Năng lực cốt lõi**: Biến PostgreSQL thành cơ sở dữ liệu Document NoSQL hoàn chỉnh (dựa trên JSONB) kết hợp Event Sourcing Engine đẳng cấp thế giới, hỗ trợ Live & Inline Projections.
- **Usecase thực tế**:
  - Lưu trữ tài liệu JSON linh hoạt không cần khai báo schema cứng, hỗ trợ tìm kiếm sâu trong JSON với chỉ mục GIN của PostgreSQL.
  - Hệ thống Event Sourcing cho ngân hàng, kế toán: Lưu trữ mọi biến động số dư dưới dạng sự kiện không thể sửa đổi (Append-only).
  - Khôi phục trạng thái đối tượng tại bất kỳ thời điểm nào trong quá khứ (Time Travel Query).

#### 25. FluentMigrator
- **Năng lực cốt lõi**: Khung quản lý phiên bản CSDL độc lập với ORM, khai báo cấu trúc bảng, cột, khóa ngoại và chỉ mục bằng mã C# Fluent rõ ràng, hỗ trợ cả Migrate Up và Rollback Down.
- **Usecase thực tế**:
  - Quản lý migration cho các dự án không dùng EF Core (ví dụ dùng Dapper, LinqToDB, ADO.NET thuần).
  - Tự động hóa cập nhật CSDL trong pipeline CI/CD trước khi deploy ứng dụng lên môi trường Production.
  - Viết migration dễ bảo trì, dễ đọc hiểu và có thể chuyển đổi giữa SQL Server, PostgreSQL, MySQL mà không cần viết lại SQL.

#### 26. DbUp
- **Năng lực cốt lõi**: Công cụ nâng cấp CSDL dựa trên việc thực thi các file SQL thuần (`.sql`) được nhúng trong assembly, ghi nhận lịch sử vào bảng `SchemaVersions`.
- **Usecase thực tế**:
  - Dành cho các đội ngũ có DBA quản lý trực tiếp câu lệnh SQL, muốn kiểm soát 100% cú pháp DDL, phân vùng bảng và chỉ mục tối ưu.
  - Tích hợp chạy nâng cấp CSDL ngay khi ứng dụng khởi động lần đầu tiên.
  - Kiểm soát tuyệt đối sự thay đổi schema mà không có bất kỳ "ma thuật" sinh code nào.

#### 27. FusionCache
- **Năng lực cốt lõi**: Thư viện bộ nhớ đệm thế hệ mới, hỗ trợ cơ chế đa tầng (L1 Memory + L2 Distributed/Redis), chống hiện tượng Cache Stampede (Distributed Locking), chế độ Fail-Safe và làm mới ngầm (Soft Timeout / Background Refresh).
- **Usecase thực tế**:
  - Các hệ thống có lượng truy cập cực lớn (High Load), ngăn chặn việc hàng ngàn request cùng chọc vào DB khi một key cache phổ biến vừa hết hạn.
  - Đảm bảo tính liên tục của dịch vụ: Khi Redis hoặc Database gặp sự cố tạm thời, FusionCache tự động trả về dữ liệu cũ (Stale Data) kèm log cảnh báo thay vì báo lỗi 500 cho người dùng.
  - Tối ưu hóa hiệu năng ứng dụng bằng cách ưu tiên đọc từ L1 RAM máy chủ trước khi gọi sang L2 Redis.

#### 28. EasyCaching
- **Năng lực cốt lõi**: Lớp trừu tượng hóa bộ nhớ đệm đa năng, hỗ trợ nhiều provider (Memory, Redis, Memcached, SQLite), hỗ trợ bộ nhớ đệm lai (Hybrid Caching) và đánh dấu cache qua Attribute.
- **Usecase thực tế**:
  - Dễ dàng chuyển đổi nhà cung cấp bộ nhớ đệm (từ Memory sang Redis) chỉ bằng cách đổi file cấu hình `appsettings.json`.
  - Tự động cache kết quả của hàm thông qua Aspect-Oriented Programming (Attribute `[EasyCachingAble]`).
  - Đồng bộ hóa việc xóa cache giữa các node trong cụm server thông qua Redis Bus.

#### 29. StackExchange.Redis
- **Năng lực cốt lõi**: Client Redis hiệu năng cao, thread-safe, sử dụng kỹ thuật Connection Multiplexing để chia sẻ một kết nối TCP duy nhất cho hàng ngàn request đồng thời, hỗ trợ đầy đủ các cấu trúc dữ liệu của Redis và Pub/Sub.
- **Usecase thực tế**:
  - Triển khai Khóa phân tán (Distributed Lock) để chống xử lý trùng lặp trong môi trường nhiều server.
  - Xây dựng bảng xếp hạng trực tiếp (Leaderboards) bằng Redis Sorted Sets.
  - Lưu trữ và đồng bộ hóa phiên làm việc của người dùng (Session Storage) và dữ liệu cache dùng chung.

#### 30. Polly v8
- **Năng lực cốt lõi**: Thư viện khả năng phục hồi (Resilience) và xử lý lỗi tạm thời tiêu chuẩn cho .NET, xây dựng trên nền tảng `ResiliencePipeline` hoàn toàn mới: Retry, Circuit Breaker, Timeout, Rate Limiter và Fallback.
- **Usecase thực tế**:
  - Tự động thử lại khi gọi API đối tác bên thứ ba gặp lỗi gián đoạn mạng hoặc HTTP 503 tạm thời với chiến lược Exponential Backoff + Jitter.
  - Ngắt mạch (Circuit Breaker) khi dịch vụ phụ thuộc bị sập kéo dài, lập tức trả về lỗi mà không làm nghẽn thread pool của hệ thống.
  - Giới hạn tốc độ gọi ra (Rate Limiter) để không vi phạm chính sách của nhà cung cấp API bên ngoài.

---

### Batch 4: Mapping, Validation, HTTP, Auth & Observability (31–40)

#### 31. AutoMapper
- **Năng lực cốt lõi**: Thư viện ánh xạ đối tượng dựa trên quy ước (Convention-based Object Mapping) phổ biến nhất trong .NET, hỗ trợ Profiles, Flattening, Projection và kiểm tra cấu hình compile/unit test.
- **Usecase thực tế**:
  - Tự động chuyển đổi giữa Domain Entities và DTOs để bảo vệ mô hình dữ liệu nội bộ không bị lộ ra ngoài API.
  - Làm phẳng (Flattening) các cấu trúc đối tượng lồng nhau phức tạp thành một DTO phẳng cho giao diện người dùng.
  - Dùng `AssertConfigurationIsValid()` trong Unit Test để đảm bảo không có trường nào bị bỏ sót khi thay đổi mô hình dữ liệu.

#### 32. Mapster
- **Năng lực cốt lõi**: Thư viện ánh xạ đối tượng hiệu năng siêu cao, sinh mã bytecode động (Expression Trees) hoặc sinh mã lúc biên dịch (Compile-time CodeGen), tốc độ nhanh hơn AutoMapper từ 3 đến 8 lần.
- **Usecase thực tế**:
  - Thay thế AutoMapper trong các ứng dụng yêu cầu hiệu năng cao và ít phân bổ bộ nhớ (Low Allocations).
  - Cú pháp cực kỳ gọn gàng với extension method `source.Adapt<TDestination>()` mà không bắt buộc phải inject `IMapper`.
  - Hỗ trợ tạo code ánh xạ trực tiếp trong lúc build dự án để đạt tốc độ tối đa tương đương code viết tay.

#### 33. FluentValidation
- **Năng lực cốt lõi**: Thư viện kiểm tra tính hợp lệ của dữ liệu (Validation) mạnh mẽ, sử dụng cú pháp Fluent C# để định nghĩa các quy tắc kiểm tra tách biệt hoàn toàn khỏi class dữ liệu.
- **Usecase thực tế**:
  - Tách rời logic kiểm tra dữ liệu ra khỏi DTOs/Entities, giữ cho model trong sạch (Clean POCO).
  - Xây dựng các quy tắc nghiệp vụ phức tạp: phụ thuộc điều kiện (`When`, `Unless`), kiểm tra bất đồng bộ với CSDL (`MustAsync` kiểm tra email đã tồn tại hay chưa).
  - Tái sử dụng các bộ quy tắc kiểm tra con (ví dụ: `AddressValidator`) trong nhiều request khác nhau.

#### 34. Refit
- **Năng lực cốt lõi**: Biến đổi một interface C# thuần túy thành một REST API Client type-safe, tự động serialize request body và deserialize response body qua `System.Text.Json`.
- **Usecase thực tế**:
  - Gọi các REST API bên ngoài (payment gateway, microservices nội bộ, mạng xã hội) mà không cần viết boilerplate code với `HttpClient`.
  - Kết hợp hoàn hảo với `IHttpClientFactory` và các chính sách phục hồi của Polly.
  - Code dễ đọc, dễ mock trong Unit Test bằng cách mock interface.

#### 35. Flurl.Http
- **Năng lực cốt lõi**: Bộ công cụ xây dựng URL theo chuỗi (Fluent URL Builder) kết hợp HTTP Client trực quan, cung cấp môi trường giả lập kiểm thử `HttpTest` không cần kết nối mạng.
- **Usecase thực tế**:
  - Xây dựng các URL phức tạp với nhiều path segments, query parameters và headers mà không bao giờ bị lỗi sai định dạng hoặc thừa/thiếu dấu `/`.
  - Viết Unit/Integration Test cho các tác vụ gọi HTTP một cách dễ dàng: `using var httpTest = new HttpTest();` và kiểm tra request đã được gửi đi chính xác.
  - Bắt lỗi HTTP tinh tế với class ngoại lệ `FlurlHttpException` chứa đầy đủ chi tiết response từ server.

#### 36. OpenIddict
- **Năng lực cốt lõi**: Khung làm việc linh hoạt, mô-đun hóa để xây dựng máy chủ cấp phép OAuth 2.0 và OpenID Connect Server trên nền tảng ASP.NET Core.
- **Usecase thực tế**:
  - Tự xây dựng giải pháp Identity Provider (IdP) nội bộ cho công ty mà không phải trả phí bản quyền đắt đỏ.
  - Cấp phát và xác thực JWT Access Token cho giao tiếp Backend-to-Backend qua Client Credentials Flow.
  - Hỗ trợ Authorization Code Flow with PKCE cho ứng dụng Single Page Apps (React, Angular) và Mobile Apps.

#### 37. Duende IdentityServer
- **Năng lực cốt lõi**: Nền tảng quản lý định danh và truy cập doanh nghiệp (Enterprise IAM) chuẩn mực nhất cho .NET, hỗ trợ đầy đủ các tiêu chuẩn OpenID Connect, OAuth 2.0, API Protection và Single Sign-On (SSO).
- **Usecase thực tế**:
  - Đăng nhập một lần (Single Sign-On - SSO) cho toàn bộ hệ thống ứng dụng Web, Di động và Desktop của doanh nghiệp.
  - Phân quyền chi tiết theo Scope và Claims cho từng API tài nguyên.
  - Quản lý phiên làm việc, thu hồi token (Token Revocation) và tích hợp liên kết với các nhà cung cấp bên ngoài (Google, Microsoft, SAML2).

#### 38. Serilog
- **Năng lực cốt lõi**: Thư viện ghi log có cấu trúc (Structured Logging) tiêu chuẩn cho .NET, lưu trữ log dưới dạng sự kiện JSON phong phú, hỗ trợ làm giàu ngữ cảnh (Enrichment) và xuất ra hàng chục Sink khác nhau.
- **Usecase thực tế**:
  - Ghi log có cấu trúc: Truy vấn log theo các thuộc tính cụ thể (`OrderId`, `CustomerId`) trên Seq, Elasticsearch hoặc Grafana Loki.
  - Tự động làm giàu dữ liệu log với `CorrelationId` để truy vết một request từ lúc vào API cho đến khi xử lý xong.
  - Cấu hình linh hoạt qua file `appsettings.json` hoặc code C#.

#### 39. NLog
- **Năng lực cốt lõi**: Thư viện ghi log hiệu năng cao, linh hoạt, hỗ trợ cấu hình bằng file XML (`NLog.config`), cung cấp hàng loạt Target (File, Database, Memory, Mail) và cơ chế tự động chia file log theo ngày/kích thước.
- **Usecase thực tế**:
  - Các hệ thống lớn yêu cầu quản lý cấu hình log tập trung qua file XML mà không cần build lại ứng dụng.
  - Sử dụng `MemoryTarget` để bắt và kiểm tra nội dung log trong các bài kiểm thử tự động (Integration Testing).
  - Tự động nén và lưu trữ các file log cũ theo thời gian (Log Archiving).

#### 40. OpenTelemetry
- **Năng lực cốt lõi**: Tiêu chuẩn mở toàn cầu cho Khả năng quan sát (Observability), hợp nhất việc thu thập Distributed Tracing (`ActivitySource`), Metrics (`Meter`) và Logs để gửi về các hệ thống giám sát qua giao thức OTLP.
- **Usecase thực tế**:
  - Truy vết phân tán (Distributed Tracing): Theo dõi toàn bộ hành trình của một request đi qua nhiều microservices để định vị chính xác điểm nghẽn hiệu năng trên Jaeger hoặc Zipkin.
  - Thu thập các chỉ số nghiệp vụ (Business Metrics) như số lượng đơn hàng, doanh thu tức thời, thời gian phản hồi và hiển thị trực quan trên Prometheus/Grafana.
  - Chuẩn hóa toàn bộ hệ thống giám sát theo chuẩn Cloud Native Computing Foundation (CNCF).

---

### Batch 5: Testing, Documentation & Utilities (41–47)

#### 41. Bogus
- **Năng lực cốt lõi**: Thư viện sinh dữ liệu giả lập (Fake Data Generator) thực tế và phong phú, hỗ trợ đa ngôn ngữ (Locales bao gồm tiếng Việt), cho phép cố định Seed để tạo dữ liệu tất định (Deterministic).
- **Usecase thực tế**:
  - Tạo hàng ngàn bản ghi dữ liệu mẫu (người dùng, địa chỉ, số điện thoại, sản phẩm, thẻ tín dụng) phục vụ Seed Database khi khởi tạo dự án.
  - Sinh dữ liệu kiểm thử thực tế cho Unit Tests và Performance Benchmarks.
  - Cố định `Randomizer.Seed` để dữ liệu sinh ra trong mỗi lần chạy test luôn giống hệt nhau, đảm bảo test không bị chập chờn (Flaky Tests).

#### 42. Verify (Verify.Xunit)
- **Năng lực cốt lõi**: Thư viện kiểm thử ảnh chụp (Snapshot Testing), tự động so sánh đối tượng kết quả với file snapshot đã được phê duyệt (`*.verified.txt`), tích hợp sẵn công cụ tự động làm sạch (Scrubbing) các dữ liệu động.
- **Usecase thực tế**:
  - Kiểm thử các cấu trúc dữ liệu JSON, XML hoặc HTML phức tạp mà không cần viết hàng chục câu lệnh `Assert.Equal` thủ công.
  - Phát hiện sớm các lỗi phá vỡ tương thích (Breaking Changes) trong response của API khi nâng cấp phiên bản phần mềm.
  - Tự động thay thế các giá trị biến động như `Guid`, `DateTime` bằng các placeholder cố định để kết quả so sánh luôn chính xác.

#### 43. FluentAssertions
- **Năng lực cốt lõi**: Thư viện viết câu lệnh khẳng định (Assertions) theo phong cách Fluent tự nhiên, cung cấp khả năng so sánh tương đương sâu (Deep Equivalency) và thông báo lỗi cực kỳ chi tiết khi test thất bại.
- **Usecase thực tế**:
  - Viết code kiểm thử dễ đọc, dễ hiểu như văn bản tiếng Anh: `result.Should().BeEquivalentTo(expected)`.
  - So sánh hai cây đối tượng phức tạp mà không cần các class phải triển khai interface `IEquatable`.
  - Tiết kiệm thời gian debug khi test fail nhờ thông báo lỗi chỉ rõ chính xác thuộc tính nào, dòng nào bị sai lệch giá trị.

#### 44. BenchmarkDotNet
- **Năng lực cốt lõi**: Thư viện chuẩn mực thế giới để đo lường hiệu năng mã nguồn .NET, tự động quản lý quá trình JIT Warmup, đo thời gian ở cấp độ nano giây và phân tích cấp phát bộ nhớ RAM (MemoryDiagnoser).
- **Usecase thực tế**:
  - So sánh hiệu năng thực tế giữa các thư viện hoặc thuật toán khác nhau (ví dụ: `System.Text.Json` vs `Newtonsoft.Json`, `for` vs `foreach` vs `LINQ`).
  - Phát hiện các điểm cấp phát bộ nhớ không cần thiết (Memory Allocations) gây áp lực lên Garbage Collector (GC Gen 0/1/2).
  - Tối ưu hóa các đoạn mã quan trọng (Hot Paths) trong các hệ thống xử lý tần suất cao.

#### 45. SpecFlow (SpecFlow.xUnit)
- **Năng lực cốt lõi**: Khung làm việc Phát triển Hướng Hành vi (Behavior-Driven Development - BDD) hàng đầu cho .NET, biến đổi các kịch bản viết bằng ngôn ngữ tự nhiên Gherkin (`Given - When - Then`) thành các bài test tự động có thể thực thi.
- **Usecase thực tế**:
  - Tạo tiếng nói chung giữa Business Analyst (BA), Tester (QA) và Lập trình viên (Developer) thông qua các kịch bản nghiệp vụ rõ ràng.
  - Xây dựng Tài liệu Sống (Living Documentation) vừa có thể đọc hiểu vừa là kiểm thử hồi quy tự động.
  - Kiểm thử chấp nhận người dùng (User Acceptance Testing) cho các luồng nghiệp vụ phức tạp như chuyển khoản ngân hàng, quy trình xét duyệt hồ sơ.

#### 46. Swashbuckle.AspNetCore
- **Năng lực cốt lõi**: Công cụ tích hợp Swagger/OpenAPI 3.0 vào ASP.NET Core, tự động sinh tài liệu JSON từ Controllers và Models, cung cấp giao diện tương tác trực quan Swagger UI, hỗ trợ đa phiên bản (Multi-versioning) và các bộ lọc tùy biến (Filters).
- **Usecase thực tế**:
  - Tự động sinh tài liệu chuẩn OpenAPI 3.0 cho toàn bộ API của dự án.
  - Quản lý và phân tách tài liệu giữa các phiên bản API khác nhau (V1, V2).
  - Tùy biến tài liệu với Operation Filters (thêm Header Correlation ID, Bearer Auth) và Schema Filters (mô tả ví dụ mẫu cho từng trường dữ liệu).

#### 47. NSwag
- **Năng lực cốt lõi**: Chuỗi công cụ OpenAPI toàn diện, vừa hỗ trợ sinh tài liệu Swagger UI và ReDoc, vừa hỗ trợ sinh mã nguồn Client SDK (C# HttpClient hoặc TypeScript Axios/Fetch) tự động từ OpenAPI Specification.
- **Usecase thực tế**:
  - Cung cấp tài liệu API hiện đại với cả 2 giao diện Swagger UI và ReDoc.
  - Tự động sinh toàn bộ code Client SDK bằng C# hoặc TypeScript cho đội ngũ Frontend/Mobile mỗi khi Backend cập nhật API, loại bỏ hoàn toàn việc viết code gọi API thủ công.
  - Tích hợp vào pipeline build để tự động xuất file SDK đồng bộ sang các kho lưu trữ khác.

---

### Batch 6: Files, Parsing & Workflow (48–52)

#### 48. Elsa Workflows
- **Năng lực cốt lõi**: Bộ khung Workflow Engine mã nguồn mở mạnh mẽ, cho phép lập trình quy trình bằng C# Code-First (`WorkflowBase`, `Sequence`, `If`, `SetVariable`) hoặc thiết kế kéo thả trực quan trên Elsa Studio, hỗ trợ cả thực thi in-process ngắn hạn lẫn quy trình phân tán dài hạn.
- **Usecase thực tế**:
  - Tự động hóa các quy trình xét duyệt nghiệp vụ: Phê duyệt đơn hàng, thẩm định hồ sơ vay vốn, quy trình tuyển dụng nhân sự.
  - Lập trình luồng nghiệp vụ phức tạp dưới dạng các bước tuần tự rõ ràng, dễ bảo trì và dễ viết unit test.
  - Quản lý các quy trình dài hạn (Long-running Workflows) cần dừng lại chờ sự kiện bên ngoài (email phê duyệt, webhook phản hồi từ đối tác).

#### 49. Stateless
- **Năng lực cốt lõi**: Thư viện máy trạng thái hữu hạn (Finite State Machine - FSM) siêu nhẹ và linh hoạt, cấu hình trạng thái và trigger bằng cú pháp Fluent C#, hỗ trợ guard clauses, entry/exit actions, lưu vết lịch sử chuyển đổi và xuất biểu đồ Mermaid/DOT.
- **Usecase thực tế**:
  - Quản lý vòng đời thực thể: Đơn hàng (`Draft` -> `Submitted` -> `UnderReview` -> `Approved` / `Rejected`), vé hỗ trợ kỹ thuật, hợp đồng điện tử.
  - Ngăn chặn triệt để các lỗi logic nghiệp vụ: Không cho phép chuyển trạng thái bất hợp lệ (ví dụ: không thể duyệt đơn hàng khi chưa gửi nộp).
  - Tự động xuất biểu đồ trạng thái (Mermaid/Graphviz) từ mã nguồn để đưa vào tài liệu kiến trúc hệ thống.

#### 50. QuestPDF
- **Năng lực cốt lõi**: Thư viện tạo tài liệu PDF theo phong cách Code-First hiện đại, sử dụng Fluent API trực quan kết hợp engine đồ họa SkiaSharp, hỗ trợ phân trang tự động, bảng biểu phức tạp, văn bản đa kiểu và tem bản quyền Community.
- **Usecase thực tế**:
  - Xuất hóa đơn bán hàng điện tử (Invoices), phiếu xuất kho, phiếu thu tiền với thiết kế chuyên nghiệp và màu sắc thương hiệu.
  - Tạo các báo cáo phân tích tài chính nhiều trang, chứng nhận hoàn thành khóa học, phiếu khám bệnh.
  - Thay thế hoàn toàn các giải pháp chuyển đổi HTML-to-PDF nặng nề, tăng tốc độ render gấp 10 lần và tiết kiệm tài nguyên máy chủ.

#### 51. ClosedXML
- **Năng lực cốt lõi**: Thư viện đọc và ghi bảng tính Excel (.xlsx) chuẩn OpenXML, cung cấp API trực quan để định dạng ô (font, màu nền, border, format tiền tệ), tính toán công thức (`=SUM(...)`, `=D2*E2`), tự động co giãn cột và đọc dữ liệu phân tích thành DTOs.
- **Usecase thực tế**:
  - Xuất báo cáo doanh thu, danh sách nhân sự ra file Excel chuyên nghiệp có sẵn công thức tính toán để người dùng tiếp tục thao tác trên Microsoft Excel.
  - Đọc và phân tích (Import) các file Excel dữ liệu lớn do khách hàng tải lên, kiểm tra tính hợp lệ từng dòng và lưu vào cơ sở dữ liệu.
  - Xử lý bảng tính trên máy chủ Linux/Docker mà không cần cài đặt Microsoft Office.

#### 52. CsvHelper
- **Năng lực cốt lõi**: Thư viện đọc và ghi file CSV chuẩn công nghiệp cho .NET, hỗ trợ tùy biến ánh xạ qua `ClassMap<T>`, xử lý luồng dữ liệu liên tục (Streaming) bất đồng bộ với dung lượng bộ nhớ cố định và tốc độ tối đa.
- **Usecase thực tế**:
  - Nhập/xuất dữ liệu khối lượng lớn (hàng triệu bản ghi danh bạ, giao dịch, lịch sử cuộc gọi) mà không làm tràn bộ nhớ RAM máy chủ.
  - Đồng bộ và trao đổi dữ liệu với các hệ thống cũ (Legacy Systems) hoặc đối tác qua định dạng CSV tiêu chuẩn.
  - Tùy biến linh hoạt định dạng ngày tháng, dấu phân cách số thập phân (`CultureInfo.InvariantCulture`) và thu thập lỗi chi tiết theo từng dòng dữ liệu hỏng.

---

## Cách chạy và kiểm thử

### 1. Chạy một bài bất kỳ
Vào thư mục API của bài đó và dùng lệnh `dotnet run`:
```powershell
cd 27-FusionCache/ProductCatalogCache.Api
dotnet run
# Mở Swagger UI tại: http://localhost:5127/swagger
```

### 2. Chạy toàn bộ unit/integration test
```powershell
dotnet test 27-FusionCache/ProductCatalogCache.slnx
```
Mọi bài test đều độc lập, tự tạo DB in-memory/temp và tự dọn dẹp sau khi chạy xong.
