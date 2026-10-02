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



