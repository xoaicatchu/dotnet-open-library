# StockFlow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xây dựng ứng dụng kho cho cửa hàng nhỏ, thao tác nhập–xuất–kiểm kê qua giao diện tiếng Việt và truy vết chính xác mọi biến động tồn.

**Architecture:** Solution .NET độc lập gồm ASP.NET Core Controller API và tests; `StockFlow.Web` là Angular PWA/npm project riêng. API stateless, chạy nhiều replica trên Kubernetes; PostgreSQL dùng chung là nguồn dữ liệu chuẩn, sổ biến động là lịch sử chuẩn và `StockBalance` là projection được cập nhật nguyên tử cùng transaction. SQLite chỉ dùng cho dev cục bộ nếu cần; tích hợp và production dùng PostgreSQL. Angular cache app shell, lưu draft/bounded outbox/cached read pages trong IndexedDB; gửi lệnh đồng bộ có `clientOperationId` ổn định và version để retry an toàn. API lấy actor từ cookie claims. Offline người dùng có thể tạo/sửa draft và nhập số kiểm kê cục bộ; xác nhận cuối cùng/post chỉ online tại server. Production UI/API cùng origin qua ingress; API fallback không được che lỗi `/api`.

**Tech Stack:** .NET 10, ASP.NET Core Controllers, EF Core 10 PostgreSQL provider, Angular standalone components, PWA service worker, IndexedDB, Router lazy feature routes, Reactive Forms, Signals cho state trang, HttpClient, Angular Material, xUnit, WebApplicationFactory, PostgreSQL integration tests bằng Testcontainers/Docker, Playwright. Không NgRx trong v1. Khóa phiên bản Angular/Node/TypeScript và package bằng lockfiles.

## Global Constraints — Context và guardrails

- Đường dẫn `128-StockFlow/`; hiện repo có `127-WASM-WASI`, chưa có 128. Theo README gốc: API Controller, solution độc lập, tests và `.http`.
- Production chạy stateless API replicas trên Kubernetes và kết nối PostgreSQL HA-managed bên ngoài cluster qua pool giới hạn; không mount SQLite/DB file chung giữa replicas. Local development dùng PostgreSQL container/compose; SQLite tùy chọn chỉ cho unit tests, không dùng để khẳng định concurrency/production behavior. CI PostgreSQL integration tests chạy bằng Testcontainers khi Docker có sẵn (hoặc service container trong CI); README nêu profile lệnh và yêu cầu Docker. Publish Angular PWA cùng API dưới một origin qua Ingress/TLS.
- Cookie auth phải hoạt động qua mọi replica: ASP.NET Core Data Protection key ring lưu bền vững trong PostgreSQL dùng chung (không ephemeral pod filesystem), với application name ổn định; production mã hóa key ring bằng certificate được mount từ Secret. Angular nhận request token chống CSRF qua endpoint và giữ trong memory để interceptor gửi header; không đọc token từ cookie hay lưu credential trong IndexedDB/localStorage.
- Migration chạy bằng một lần Kubernetes Job trong release trước rollout API, tuyệt đối không để từng pod tự migrate. ConfigMap chứa cấu hình không bí mật; DB credentials/cookie secrets qua Kubernetes Secret hoặc external secret manager. API có startup/readiness/liveness probes, graceful shutdown, resource requests/limits, structured logs, metrics/traces. HPA cấu hình thận trọng theo tải thực đo và giới hạn kết nối PostgreSQL; không claim tải lớn nếu chưa có load target/benchmark.
- PostgreSQL transaction là ranh giới atomic cho trạng thái phiếu, balance và movements. Cập nhật tồn bằng conditional SQL (`quantity >= requested` và version), unique constraints/idempotency keys; trong phiếu nhiều dòng xử lý product theo thứ tự ID ổn định và rollback toàn phiếu khi một dòng xung đột. Draft create/update/post và đổi unit khóa product row theo thứ tự ID ổn định; không đổi unit khi còn draft đang tham chiếu hoặc đã có movement. Mọi replica dùng cùng chính sách này; báo cáo đọc từ journal/balance theo mục tiêu consistency đã nêu.
- Một kho; số lượng nguyên dương `long`, một đơn vị cố định trên mỗi mặt hàng. Không đổi đơn vị khi có movement hoặc draft đang tham chiếu; vô hiệu hóa hàng thay cho xóa hàng đã sử dụng. Tiền VND nguyên không âm, lưu `long`; phép tính dùng checked. Mọi số nguyên JSON phải có `abs(value) <= Number.MAX_SAFE_INTEGER` (9007199254740991), gồm quantity, version, tiền, delta có dấu, tổng tiền phiếu và aggregate/net báo cáo. Kiểm tra đầu vào lẫn kết quả trước commit; vượt giới hạn trả400, rollback thay vì làm tròn. Frontend validate số nguyên an toàn; test biên cộng/nhân/version/tổng báo cáo.
- Nhập đầu kỳ cũng qua phiếu nhập; không sửa trực tiếp tồn. Không cho tồn âm, kể cả khi hủy phiếu nhập/điều chỉnh tăng.
- Trạng thái `Draft`, `Posted`, `Cancelled`. Phiếu Posted bất biến. Hủy Posted tạo bút toán đảo, giữ phiếu gốc; Draft hủy không sinh bút toán. Gửi lại post/cancel cùng phiếu trả kết quả hiện tại, không sinh thêm bút toán. Post phiếu Cancelled trả 409.
- `PostedAt` và `CancelledAt` do server ghi UTC; không nhập lùi ngày. Ngày nghiệp vụ ghi chú không thay đổi mốc báo cáo.
- Offline: PWA cache chỉ app shell/static assets, không cache credential hoặc response mutation/private API trong Service Worker. IndexedDB giữ bản nháp và bounded FIFO outbox; không cần change-feed cursor trong v1. Khi online, tải lại catalog/balance qua REST pagination, cập nhật cache từng trang và ghi thời điểm; data cache offline chỉ là context có thể cũ. Thao tác offline hiển thị trạng thái “Chưa đồng bộ”. Đồng bộ dùng `clientOperationId` bền vững/duy nhất và request fingerprint; retry cùng ID/cùng payload trả kết quả cũ, cùng ID/khác payload trả409. Conflict version hoặc tồn kho trả409, giữ dữ liệu local và yêu cầu người dùng review. Không xác nhận phiếu Posted offline: nhiều thiết bị tách mạng không thể đảm bảo không oversell nếu chưa có quota/reservation phân bổ trước. Quota, tự động merge và background sync khi app đóng được hoãn; lần mở app/kết nối lại sẽ sync.
- V1 lưu đơn giá nhập và tổng tiền phiếu nhập, không tính giá vốn, lợi nhuận hay giá trị tồn. Bình quân gia quyền cần chính sách riêng cho xuất, hủy và kiểm kê; hoãn khỏi v1.
- Admin quản lý danh mục/tài khoản; Warehouse lập, xác nhận, hủy phiếu và kiểm kê; Viewer chỉ đọc. Actor lấy từ identity server-side. Không có tài khoản/mật khẩu yếu mặc định; demo seed chỉ bật Development với mật khẩu cấu hình ngoài source.
- Không CQRS, broker/event bus, nhiều kho, chuyển kho, lô/hạn sử dụng, quy đổi đơn vị, công nợ, thanh toán hay tích hợp hãng vận chuyển trong v1. Không có offline stock reservation/quota; các lệnh tồn kho offline là draft chờ server.

## Dữ liệu và hợp đồng

`Product(Id, Sku unique normalized, Barcode nullable unique (trim; chuỗi rỗng thành null), Name, Unit, PurchasePriceVnd, SalePriceVnd, MinimumStock, IsActive)`; `Supplier(Id, Code unique, Name, Phone)`.

`StockDocument(Id, Number unique, Type Receipt/Issue/Stocktake, Status, SupplierId?, Reason?, CreatedAtUtc, PostedAtUtc?, CancelledAtUtc?, CreatedBy, PostedBy?, CancelledBy?, Version)`; `StockDocumentLine(Id, DocumentId, ProductId, Quantity, UnitCostVnd?, CountedQuantity?, ObservedBalanceVersion?, SnapshotQuantity?, SnapshotBalanceVersion?)`. `ObservedBalanceVersion` records the balance version the counter saw when counting began; the server validates it before capturing its own authoritative snapshot.

`StockBalance(ProductId PK, Quantity >= 0, Version)`; `StockMovement(Id, DocumentId, LineId, ProductId, Delta, Kind Receipt/Issue/Adjustment/Reversal, OccurredAtUtc, ActorId, ReversesMovementId?)`. Unique movement gốc theo line; unique `ReversesMovementId` khi có giá trị. FK restrict xóa dữ liệu đã sử dụng. Tạo balance bằng 0 khi tạo product; ghi nhận cả kiểm kê chênh lệch 0 để khóa lịch sử phiếu, không làm tăng tổng nhập/xuất.

Thêm `ClientOperation(UserId, ClientOperationId, OperationType, ResourceId, RequestHashSha256, ResultStatus, ResultJson, ProcessedAtUtc)` với unique `(UserId, ClientOperationId)` để retry sync trả kết quả đã lưu; giới hạn kích thước batch/payload. Hash bao gồm operation type, resource/document ID, expected version và canonical payload; cùng ID/cùng hash replay kết quả, cùng ID/khác hash trả409 `IdempotencyKeyReused`. Draft tạo từ client dùng UUID ổn định. V1 không tạo global sequence/change feed; khi online client tải lại dữ liệu tra cứu bằng REST pagination và thay cache từng trang, còn phản hồi operation push trả kết quả authoritative cho chính draft đó. Queue cục bộ gắn với user ID lấy từ phiên online gần nhất; khi sync phải đăng nhập lại đúng user, server vẫn ghi actor từ claims hiện tại. Không cho một user gửi hàng đợi của user khác.

API dưới `/api`: auth GET csrf, POST login/logout, GET me, Admin-only GET/POST users; GET csrf lưu antiforgery cookie HttpOnly và trả request token trong JSON; Angular giữ token trong memory và custom interceptor gửi `X-XSRF-TOKEN` cho mutation cùng origin. Không dùng JWT/localStorage hay interceptor XSRF mặc định đọc token từ cookie. Mọi mutation xác thực cookie/CSRF và actor từ claims. products/suppliers GET/POST/PUT (Admin); documents GET/detail/POST draft/PUT draft/POST `{id}/post`/POST `{id}/cancel`; stocktakes POST/PUT/refresh snapshot; balances/movements/reports. Thêm endpoint đồng bộ bounded batch: client đẩy các draft operation theo thứ tự kèm stable `clientOperationId` và `expectedVersion`, server trả kết quả từng lệnh (accepted/current result hoặc conflict). Không có generic delta-pull endpoint trong v1; online data refresh dùng REST pagination. Server unique index trên operation ID bảo đảm retry idempotent; fingerprint khác payload trả409. Không cho sync bypass state machine/permissions/transaction.

DTO phiếu nhận `version` và danh sách product không trùng, quantity > 0 với Receipt/Issue, countedQuantity >= 0 với Stocktake. Receipt bắt buộc supplier và unitCostVnd; Issue bắt buộc reason Sale/SupplierReturn/Damage/Other. SupplierReturn bắt buộc supplier. 400 validation, 401 chưa đăng nhập, 403 sai quyền, 404 thiếu ID, 409 sai trạng thái/thiếu tồn/snapshot cũ/xung đột version; PostgreSQL transient/pool timeout sau retry trả503 ProblemDetails có mã ổn định `DatabaseTemporarilyUnavailable`, còn lỗi constraint không bị gộp thành lỗi tạm thời. List phân trang mặc định 20, tối đa 100.

## Task flow: 6 bước

### Task 1 — Solution, database và đăng nhập

**Files:** `128-StockFlow/StockFlow.slnx`; `StockFlow.Api/{StockFlow.Api.csproj,Program.cs,appsettings.json,Properties/launchSettings.json,Data/StockFlowDbContext.cs,Data/Migrations/,Data/DevelopmentSeeder.cs,Auth/{AuthController.cs,UsersController.cs,UserDtos.cs,RolePolicies.cs}}`; `StockFlow.Web/{package.json,package-lock.json,angular.json,proxy.conf.json,src/main.ts,src/app/app.config.ts,src/app/app.routes.ts}`; `StockFlow.Tests/{StockFlow.Tests.csproj,Infrastructure/StockFlowApiFactory.cs,AuthTests.cs,SchemaTests.cs}` (mọi đường dẫn con dưới `128-StockFlow/`). Lưu Data Protection key ring trong PostgreSQL để mọi pod dùng chung cookie key.

- [ ] Parent đã quét repo không thấy 5166/5167; chọn API localhost:5166 và Angular localhost:4200 development, kiểm tra listener trước khi thực hiện.
- [ ] Tạo net10.0 solution độc lập, PostgreSQL migrations và cookie Identity, Angular SPA/PWA qua proxy development, build frontend vào `wwwroot`; dùng antiforgery cookie/header, không tự tạo password hashing. Local chạy PostgreSQL bằng compose; cấu hình rõ lệnh env/config, không chứa secret trong appsettings.
- [ ] Định nghĩa schema ở trên với check/unique/FK; seed Admin/Warehouse/Viewer chỉ khi Development và secret do operator cung cấp. Seed hàng mẫu, tồn đầu kỳ qua luồng phiếu nhập sau khi Task 2 hoàn thành. Thêm lệnh `--bootstrap-admin` one-shot cho production, yêu cầu email/password cấu hình ngoài image và không chạy API sau bootstrap.
- [ ] Tests: chưa login GET `/api/auth/me` →401; migration trên DB rỗng thành công; cấu hình production không tự sinh tài khoản demo; thiếu/sai CSRF bị từ chối; token sau login/logout dùng được; hai test hosts dùng chung PostgreSQL vẫn xác thực cookie đã phát từ host còn lại; bootstrap thiếu secret fail closed; SPA deep link hoạt động và `/api/unknown` trả404. Kiểm tra quyền Viewer và SKU trùng cùng CRUD ở Task 2 khi products endpoint tồn tại.

**Acceptance:** Restore/build solution độc lập; auth/permission/schema và migration tests chạy trên PostgreSQL thật bằng Testcontainers/service container; SQLite nếu dùng chỉ cho isolated unit tests, không EF InMemory cho integration. API không trả password hash.

### Task 2 — Danh mục, nhập kho và sổ biến động

**Files:** `StockFlow.Api/Products/{ProductsController.cs,ProductDtos.cs}`, `Suppliers/{SuppliersController.cs,SupplierDtos.cs}`, `Documents/{DocumentsController.cs,DocumentDtos.cs,StockDocumentService.cs}`, `Inventory/{InventoryTransactionRunner.cs,StockMovementWriter.cs}`, `Sync/{SyncController.cs,SyncService.cs,ClientOperationStore.cs}`; `StockFlow.Tests/{CatalogTests.cs,ReceiptTests.cs,SyncIdempotencyTests.cs}`.

**Interface:** `PostAsync(Guid documentId, long expectedVersion, string actorId, CancellationToken)` trả chi tiết phiếu; `CancelAsync` cùng chữ ký. Dùng TimeProvider cho timestamp kiểm thử.

- [ ] Viết tests trước: Draft receipt 100 không tăng tồn; Post tăng 100 và một movement; gọi Post lần hai vẫn 100; sửa Posted →409; phiếu hai dòng với dòng lỗi rollback toàn bộ.
- [ ] Implement validation và CRUD danh mục/draft, chặn sửa đơn vị khi đã có movement hoặc draft đang tham chiếu; lock product row ổn định cho draft create/update/post và đổi unit để quantity không đổi đơn vị giữa lúc nhập và xác nhận. Ngừng sử dụng product bằng IsActive, không xóa hàng/nhà cung cấp đã được tham chiếu. Từ chối dòng product trùng và chặn thêm hàng inactive vào phiếu mới, vẫn cho xem lịch sử/đảo phiếu cũ.
- [ ] Trong transaction, conditional update trạng thái `WHERE Id=@id AND Status='Draft' AND Version=@version`; cập nhật balance + version, ghi journal, commit tất cả. Re-read trạng thái để phân biệt repeat thành công và stale edit; không tin trạng thái đọc trước transaction.
- [ ] PostgreSQL transient connection/serialization/deadlock retry: transaction strategy retry toàn bộ command tối đa có giới hạn và jitter/cancellation; unique idempotency key ngăn ghi lặp. Không retry validation/409. Theo dõi connection pool timeout; không retry vượt budget request.

**Acceptance:** PostgreSQL restart giữ tồn; mỗi Posted line có đúng một movement; tổng journal từng product bằng balance; giá và tổng tiền phiếu nhập chính xác, overflow trả400 và rollback. Concurrent unit update không làm thay đổi đơn vị của draft đang post; lỗi DB transient hết retry trả503 ổn định. Viewer không thể tạo product; Admin tạo product thành công và SKU trùng bị từ chối.

### Task 3 — Xuất kho, hủy và bảo vệ đồng thời

**Files:** `StockFlow.Api/Documents/StockDocumentService.cs`, `StockFlow.Api/Inventory/{StockMovementWriter.cs,InventoryTransactionRunner.cs}`; `StockFlow.Tests/{IssueTests.cs,CancellationTests.cs,ConcurrentPostingTests.cs}`.

- [ ] Test receipt 100 → issue 12 → damage 2 → balance 86; xuất 87 bị409; toàn bộ phiếu nhiều dòng rollback khi một mặt hàng thiếu tồn.
- [ ] Trừ tồn bằng DB conditional update: `UPDATE StockBalances SET Quantity=Quantity-@qty, Version=Version+1 WHERE ProductId=@id AND Quantity>=@qty`; affected rows phải bằng1. Không dùng đọc tồn rồi trừ trong memory làm cơ chế duy nhất.
- [ ] Hủy tạo movement đối dấu, linked original, timestamp lúc hủy; đảo nhập chỉ thành công nếu đủ tồn. Hủy lặp lại không thêm movement; hủy phiếu xuất hoàn lại tồn; hủy Draft không ảnh hưởng kho.
- [ ] Chạy hai HTTP clients đồng thời xuất 7 từ tồn10: đúng một200, một409, tồn3. Hai clients post cùng một phiếu: hai kết quả idempotent hợp lệ, tồn chỉ biến động một lần. Test hủy nhập100 sau đã xuất12 →409, phiếu vẫn Posted.

**Transaction pseudocode chung:**

```text
retry lỗi PostgreSQL transient/deadlock trong budget giới hạn:
  begin transaction
  đọc phiếu; nếu đã ở trạng thái yêu cầu: trả existing, không thêm movement
  conditional claim trạng thái/version; sai trạng thái hoặc version: rollback,409
  kiểm tra toàn bộ dữ liệu và snapshot kiểm kê trong transaction
  mỗi dòng: conditional update balance/version; affected != 1: rollback,409
  thêm movement (hoặc reversal unique), stamp server UTC và actor
  commit; bất kỳ lỗi dòng nào: rollback toàn phiếu
```

**Acceptance:** Không tồn âm, không partial commit, không duplicate movements khi concurrent/retry; lỗi cuối cùng trả mã ổn định để UI giải thích được.

### Task 4 — Kiểm kê và báo cáo nhập–xuất–tồn

**Files:** `StockFlow.Api/Stocktakes/{StocktakesController.cs,StocktakeDtos.cs,StocktakeService.cs}`, `Reports/{ReportsController.cs,StockReportService.cs,StockReportDtos.cs}`; `StockFlow.Tests/{StocktakeTests.cs,StockReportTests.cs}`.

- [ ] Stocktake line lưu countedQuantity và `ObservedBalanceVersion` mà người dùng nhìn thấy khi bắt đầu đếm; với đếm offline, lấy version từ cached balance tại thời điểm mở màn kiểm kê. Khi sync, server không nhận snapshot quantity làm sự thật: cùng transaction phải so version client với balance authoritative, nếu lệch trả409 và giữ nguyên local count. Nếu khớp, server mới lưu authoritative snapshot quantity/version vào draft. Khi Post, so lại snapshot version; bất kỳ dòng stale →409 và rollback toàn phiếu. Refresh online phải yêu cầu người dùng đếm/xác nhận lại từng dòng bị ảnh hưởng trước khi cập nhật snapshot.
- [ ] Adjustment = counted - snapshot; có thể âm/dương/0, không sửa phiếu Posted. Hủy kiểm kê đảo adjustment và áp dụng điều kiện tồn không âm.
- [ ] UI nhận ngày theo `Asia/Ho_Chi_Minh` rồi đổi sang UTC, API dùng khoảng `[fromUtc,toUtc)`: một câu SQL aggregate duy nhất đọc journal tại một snapshot DB cho opening=sum trước from, receipt net theo receipt + reversal của receipt, issue net theo issue + reversal của issue (hiển thị lượng xuất dương), adjustment net riêng; closing tính từ cùng các aggregate. Bút toán hủy thuộc kỳ hủy, không sửa kỳ gốc.
- [ ] Test tồn86/version7 đếm85 offline, thiết bị khác xuất2 làm version8 → sync trả409, không tạo delta +2; kiểm kê stale khi post trả409; kiểm kê đồng thời xuất không ghi đè tồn. Test report trong lúc posting phải lấy mọi aggregate từ cùng statement snapshot. Test receipt kỳ1/hủy kỳ2: kỳ1 giữ100, kỳ2 receipt net -100; boundary UTC chính xác; aggregate/net vượt miền số nguyên an toàn trả400.

**Acceptance:** Dashboard/tồn thấp dùng balance; report và lịch sử dùng journal; mọi kỳ reconciliation khớp; không trình bày tổng tiền tồn/giá vốn.

### Task 5 — Giao diện Angular offline-capable

**Files:** `StockFlow.Web/src/app/core/{auth,api,interceptors,guards,models,offline}/`, IndexedDB stores/schema, `src/app/shared/`, `src/app/features/{auth,dashboard,products,suppliers,receipts,issues,stocktakes,reports}/`, `src/app/{app.config.ts,app.routes.ts}`, `ngsw-config.json`, `src/styles.scss`, colocated `*.spec.ts`, `StockFlow.Web/{playwright.config.ts,e2e/inventory.spec.ts,e2e/auth.spec.ts,e2e/offline-sync.spec.ts}`. Standalone components, lazy feature routes, Reactive Forms, Signals, Angular Material.

- [ ] Xây navigation tiếng Việt: Tổng quan, Hàng hóa, Nhà cung cấp, Nhập kho, Xuất kho, Kiểm kê, Báo cáo. Badge Nháp/Đã xác nhận/Đã hủy; số lượng/VND đúng locale, UTC đổi giờ địa phương.
- [ ] Form nhập/xuất tìm SKU/mã vạch, thêm dòng, số lượng/đơn giá/tổng tiền; validate số nguyên an toàn. Posted chỉ đọc, dialog xác nhận hủy/lý do. Disable nút khi gửi, API vẫn bảo đảm idempotency.
- [ ] Stocktake hiển thị snapshot/thực đếm/chênh lệch, lỗi stale yêu cầu refresh và nhập lại. Report có kỳ, opening/receipt/issue/adjustment/closing và lịch sử từng hàng.
- [ ] Auth service lấy csrf trước login, refresh sau login/logout, GET me phục hồi session. Giữ request token trong memory; custom HttpInterceptor gửi `X-XSRF-TOKEN` chỉ với unsafe same-origin `/api` calls; không dùng default cookie-reading XSRF interceptor, không lưu auth token/CSRF token bền vững. Guard/UI theo role, API luôn xác minh claims/policy.
- [ ] PWA service worker cache app shell/static files only. IndexedDB stores local drafts, bounded FIFO outbox, cached catalog/balance with observed versions and timestamps; không có change-feed cursor. Stores no credentials and no private API responses in CacheStorage. Generate stable UUID document IDs and `clientOperationId` before enqueue; retries reuse ID only with byte-equivalent semantic payload; unsent edits coalesce to a new ID. Show offline banner, last-sync time, pending count, and per-item states `Chưa đồng bộ`, `Đã đồng bộ`, `Cần xử lý`.
- [ ] Offline allow viewing explicitly cached catalogs/balances with stale timestamp and creating/editing drafts plus stocktake counts that preserve each observed balance version. Never label cached stock as current. Disable Post/Cancel while offline; queue draft sync only. Reconnect/app-open refreshes current REST pages and pushes bounded batches; on 409 retain the local copy, show server/local versions and require user review/reapply with a new operation ID. No automatic merge of quantity postings and no background sync assumption.
- [ ] Angular unit tests dùng HttpTestingController kiểm tra URL/payload/auth workflow/CSRF memory-header/401/403/409; Reactive Forms validation, hàng trùng, số không an toàn, lỗi409 giữ input, loading kết thúc khi lỗi, Viewer không có nút mutation. Cấu hình test target/runner thực sự hỗ trợ `npm test -- --watch=false` cho phiên bản Angular chọn.
- [ ] Playwright chạy API với PostgreSQL container, user/role seed bằng test-only config; webServer quản lý API/Angular. Test role/CSRF/login, online walkthrough100→86→85, offline app-shell load, tạo draft/stocktake khi offline, restart browser không mất outbox, reconnect idempotent sync, cached stocktake version stale after another device issue →409 and preserve count, forced 409 giữ dữ liệu và yêu cầu review. Verify browser desktop/mobile: loading/error/empty/offline/stale states, focus/labels.

**Acceptance:** Hoàn tất luồng online bằng Angular UI; khi mất mạng UI mở được, draft/kiểm kê lưu qua reload, reconnect sync đúng một lần và conflict có thể review. Post chính thức chỉ thực hiện khi server online. Unit/E2E chạy được.

### Task 6 — Hoàn thiện tài liệu và xác minh độc lập

**Files:** `128-StockFlow/{README.md,StockFlow.http,.gitignore,Dockerfile,compose.yaml,deploy/k8s/}`; cập nhật một mục `README.md` gốc; `StockFlow.Tests/EndToEndInventoryTests.cs`.

- [ ] README tiếng Việt mô tả domain, offline semantics/conflicts, schema, state/transaction/concurrency/idempotency, role, cổng thực tế, dev PostgreSQL, Docker/test profile, Angular PWA, local run, Kubernetes deployment, ingress/config/secrets/probes/migration Job/observability and caveat: offline post is provisional/disabled. Include production `--bootstrap-admin` one-shot, 100→86→85 and offline/reconnect demo. `.http` covers login/CSRF, users, sync retry, post/cancel/stocktake/report/409.
- [ ] Chạy từ repo root:

```powershell
dotnet restore 128-StockFlow/StockFlow.slnx
dotnet build 128-StockFlow/StockFlow.slnx -c Release --no-restore
dotnet test 128-StockFlow/StockFlow.slnx -c Release --no-build
dotnet run --project 128-StockFlow/StockFlow.Api

```

- [ ] Development hai terminal: terminal1 chạy API ở trên; terminal2 chạy `npm ci` rồi `npm start` trong StockFlow.Web. Define start = `ng serve --port 4200 --proxy-config proxy.conf.json`; browser mở localhost:4200, relative /api dùng proxy, không CORS rộng.
- [ ] Define npm scripts test = `ng test`, build = `ng build --configuration production`, test:e2e = `playwright test`; install browser bằng `npx playwright install chromium`. Verify trong StockFlow.Web:

```powershell
npm ci
npm test -- --watch=false
npm run build
npm run test:e2e
```

- [ ] Production cấu hình Angular outputPath `dist/stockflow`; kiểm tra index.html tại `dist/stockflow/browser`, copy assets vào API wwwroot trước publish:

```powershell
# Chạy trong 128-StockFlow/StockFlow.Web
npm ci
npm run build
New-Item -ItemType Directory -Force -Path ../StockFlow.Api/wwwroot
Copy-Item -Path ./dist/stockflow/browser/* -Destination ../StockFlow.Api/wwwroot -Recurse -Force
dotnet publish ../StockFlow.Api/StockFlow.Api.csproj -c Release -o ../artifacts/publish
```

Build immutable API image có Angular assets và chạy nhiều replicas; database là PostgreSQL HA-managed ngoài cluster. `deploy/k8s/` có Deployment, Service, Ingress/TLS, ConfigMap, Secret reference, resource requests/limits, startup/readiness/liveness probes, termination grace period, conservative HPA and one-shot migration Job. Không commit secrets; không cho pods chạy migration lúc startup. Smoke app shell offline, deep links, login/CSRF, API 404, restart/rollout và DB continuity.

- [ ] Live smoke trên PostgreSQL: login, sync retry cùng operation ID, nhiều dòng atomic rollback, post lặp, 409 thiếu tồn/stale stocktake, báo cáo/hủy. Concurrent requests từ nhiều API replicas cho hai xuất 7 từ tồn10 phải ra một success/một409 và tồn3. Roll restart mọi replica; dữ liệu journal/balance phải còn và nhất quán.
- [ ] Verify Kubernetes probes, graceful termination, migration Job once, logs/metrics/traces and config/secret wiring locally (kind/minikube if available). Run a bounded load test with documented target and DB connection budget; report measured throughput/latency and bottleneck, no unsupported “large-scale” claim.
- [ ] Reviewer/verifier riêng kiểm tra state machine, SQL conditional updates, rollback và UI; sửa lỗi rồi chạy lại checks liên quan. Không commit/push trong giai đoạn lập plan; lúc thực hiện tuân theo yêu cầu giao hàng được user xác nhận.

**Acceptance:** Build/tests and PostgreSQL integration/E2E pass; offline queue survives reload and syncs idempotently; multi-replica concurrency passes; Kubernetes manifests/probes/migration Job are smoke-tested; bounded load results and limits documented. README lets a reader run locally and deploy to a Kubernetes cluster with PostgreSQL. Root README only adds this project entry.

## Acceptance matrix

| Tình huống | Kết quả |
|---|---|
| Nháp nhập100 | Tồn0, chưa có journal |
| Post nhập100 hai lần | Tồn100, một bộ journal |
| Xuất12 và hỏng2 | Tồn86, hai phiếu truy vết |
| Phiếu nhiều dòng thiếu một hàng | 409, không dòng nào thay đổi |
| Hai xuất7 đồng thời từ10 | Một thành công, một409, tồn3 |
| Kiểm kê86→85 | Adjustment -1; báo cáo tồn85 |
| Snapshot kiểm kê cũ | 409, giữ Draft, không ghi journal |
| Hủy nhập gây âm | 409, phiếu vẫn Posted |
| Hủy xuất hai lần | Hoàn tồn một lần |
| Hủy ở kỳ sau | Kỳ trước giữ nguyên; kỳ sau ghi reversal |
| Viewer gọi mutation trực tiếp | 403 |
| Offline tạo draft, refresh tab | Draft còn trong IndexedDB, trạng thái chờ đồng bộ |
| Reconnect retry cùng `clientOperationId` | Một kết quả server, không tạo bản ghi/bút toán trùng |
| Offline bật Post | Nút bị vô hiệu; phiếu chỉ Posted sau server xác nhận online |
| Sync version/tồn xung đột | 409, giữ bản local, người dùng review trước khi gửi lại |
| Nhiều replica cùng xuất 7 từ tồn10 | Một thành công, một409, tồn3 |
| Rolling restart replicas | PostgreSQL giữ journal/trạng thái/tồn; client sync tiếp được |

## Success criteria và quyết định mở

Tài liệu tham khảo: [EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency), [Angular service worker](https://angular.dev/ecosystem/service-workers), [Angular security/XSRF](https://angular.dev/best-practices/security), [Kubernetes probes](https://kubernetes.io/docs/concepts/configuration/liveness-readiness-startup-probes/), [Kubernetes Jobs](https://kubernetes.io/docs/concepts/workloads/controllers/job/), [Kubernetes HPA](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/). Dùng application-managed Version kết hợp guarded SQL/transaction; PostgreSQL integration tests là bằng chứng concurrency, không SQLite tests.

Hoàn thành ứng dụng một kho Angular/.NET vận hành được khi offline cho draft/kiểm kê, đồng bộ idempotent, PostgreSQL source of truth, stateless replicas trên Kubernetes và giao dịch kho không oversell dưới concurrency. Chỉ việc xác nhận tồn toàn cục yêu cầu kết nối API. Phương án thực hiện: theo 6 task, nghiệm thu với PostgreSQL thật, browser offline/reconnect và Kubernetes rollout.



