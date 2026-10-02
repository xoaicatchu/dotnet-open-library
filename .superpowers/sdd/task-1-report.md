# Task 1 StockFlow — implementation report

Status: DONE_WITH_CONCERNS. Commit: none. Không stage/commit, không thay đổi branch/HEAD hoặc file ở project khác. Ngoài 128-StockFlow, chỉ report này là ghi theo phân công.

## Đã thực hiện

- Solution .NET10 độc lập; pinned EF/Identity/DataProtection10.0.12 và Npgsql EF10.0.3.
- PostgreSQL17 migration từ DB rỗng: Identity, shared DataProtectionKeys, catalog, documents/lines, balances, movements và ClientOperations (RequestHashSha256). Không SyncChange/delta cursor. ObservedBalanceVersion và snapshot version có check safe integer. Restrict FK, uniqueness và quantity/version checks.
- Cookie Identity HttpOnly/SameSiteStrict; secure trong Production. Global auto antiforgery cho controllers. GET csrf trả request token JSON; Angular giữ memory, custom interceptor unsafe same-origin /api và refresh sau login/logout; tắt cookie-reader interceptor mặc định.
- Admin-only user provisioning/pagination; Admin/Warehouse/Viewer roles. Development account seed chỉ khi operator cung cấp secret; Production không seed demo. --migrate riêng, API replica không tự migrate. --bootstrap-admin fail-closed, không overwrite tài khoản và return trước HTTP Run.
- DataProtection application name StockFlow, key ring PostgreSQL; Production API bắt buộc PFX Secret, dùng certificate encryption. Cross-host cookie test và encrypted key XML test đã pass.
- Angular21 standalone shell/login/lazy routes/Material, localhost4200 proxyAPI5166; PWA cache app shell/static assets only, không /api. dotnet publish chạy npmci+build rồi đóng bundle vào wwwroot. Local PostgreSQL compose không commit credential; hướng dẫn ở docs/development.md.

## TDD RED

1. Chỉ generated .NET skeleton trước production feature. `dotnet test StockFlow.slnx --logger 'console;verbosity=minimal'` → exit1, Failed5/Passed0. PostgreSQL Testcontainers17 chạy thật. Expected failures: schema expected6 tables actual0; auth/me expected401 actual404; login missing/wrong-CSRF expected400 actual404; csrf expected200 actual404. Không lỗi compile/setup.
2. Sau Angular generated shell, sửa expectation app heading thành StockFlow. `npm test -- --watch=false` với Vitest threads → exit1, Failed1/Passed1; expected StockFlow, received Hello,StockFlow.Web. Đây là RED UI behavior trước shell implementation.

## GREEN và verification cuối

- `dotnet restore StockFlow.slnx` → exit0.
- `dotnet build StockFlow.slnx -c Release --no-restore` → exit0, 0warnings/0errors.
- `dotnet test StockFlow.slnx -c Release --no-build --logger 'console;verbosity=minimal'` → exit0, Passed12/Failed0/Skipped0. Bao gồm anonymous401, missing/wrong-CSRF400, JSONcsrf, identity-bound token refresh, two-host cookie, no password hash response, Viewer403/Admin201 provisioning, bootstrap fail-closed+admin role+duplicate refusal, Production no-demo/encrypted ring, one-shot subprocess fail nonzero/no listening, SPA deep link/API404, empty-database migrations/check constraints/ObservedBalanceVersion/negative-price DB rejection.
- `npm test -- --watch=false` → exit0, 2test files/5tests pass: shell plus memory CSRF only unsafe same-origin, absent-token block, refresh on login/logout.
- `npm run build` → exit0, production bundle initial272.30kB/estimated75.09kB, lazy login/dashboard.
- `dotnet publish StockFlow.Api -c Release --no-restore -o artifacts/publish` → exit0; npmci10.9.4 installed473packages, audit0; Angular build passes; wwwroot contains index.html/ngsw.json/ngsw-worker.js/manifest.webmanifest. Inspected ngsw dataGroups empty and navigation excludes /api.
- Port check before app work: no listener on5166/5167/4200. No persistent API/DB dev processes started; Testcontainers disposed after tests.
- Build-level C#/TypeScript diagnostics pass. lsp_diagnostics tools are unavailable in this session. No TODO/HACK/debugger/console.log in authored API/frontend source. git diff --check has no errors (project files remain untracked, so compiler/Angular build are substantive checks).

## Installer issues and resolved concerns

- Node22.22.1 cannot run Angular22 current CLI (requires22.22.3). Locked Angular core21.2.25, CLI/build21.2.24, Material/CDK21.2.14, Vitest4.1.11; compatible with Node22.22.1.
- npm10.9.4 initial install hit arborist `Cannot read properties of null (reading edgesOut)` during optional Vitest peer graph resolution. Locked versions + fresh npm11.12.0 resolution solved it; subsequent npmci10.9.4 during publish succeeds.
- Audit initially found vulnerable older patches. Updated supported Angular21 patches and overrode compatible piscina5.3.2 patch; final npm audit0. Lockfile included.
- Windows Vitest fork workers timed out. Explicit threads/maxWorkers1 config solves it; old task-owned fork processes stopped after command-line identification.
- Automatic approval blocked verified local recursive cleanup (reason only blocked by policy). Following parent instruction not to retry deletion, renamed only generated node_modules/old lock within this new subtree instead. Ignored .install-backup and package-lock.initial.json remain for reversible cleanup; they are not published/source changes.
- Production certificate rotation needs an operator rollout policy retaining ability to decrypt old keys; single active Secret certificate is implemented for v1. Inventory business endpoints/offline IndexedDB feature pages/Kubernetes manifests are later tasks, not claimed here.

## Files created/changed

- `128-StockFlow/dotnet-tools.json`
- `128-StockFlow/.gitignore`
- `128-StockFlow/compose.yaml`
- `128-StockFlow/StockFlow.slnx`
- `128-StockFlow/docs/development.md`
- `128-StockFlow/StockFlow.Api/appsettings.json`
- `128-StockFlow/StockFlow.Api/appsettings.Development.json`
- `128-StockFlow/StockFlow.Api/StockFlow.Api.csproj`
- `128-StockFlow/StockFlow.Api/Auth/UsersController.cs`
- `128-StockFlow/StockFlow.Api/Auth/UserDtos.cs`
- `128-StockFlow/StockFlow.Api/Auth/RolePolicies.cs`
- `128-StockFlow/StockFlow.Api/Auth/AuthController.cs`
- `128-StockFlow/StockFlow.Web/vitest.config.ts`
- `128-StockFlow/StockFlow.Web/tsconfig.spec.json`
- `128-StockFlow/StockFlow.Web/tsconfig.json`
- `128-StockFlow/StockFlow.Web/tsconfig.app.json`
- `128-StockFlow/StockFlow.Api/Properties/launchSettings.json`
- `128-StockFlow/StockFlow.Api/Program.cs`
- `128-StockFlow/StockFlow.Web/.editorconfig`
- `128-StockFlow/StockFlow.Web/.gitignore`
- `128-StockFlow/StockFlow.Tests/AuthTests.cs`
- `128-StockFlow/StockFlow.Web/.vscode/tasks.json`
- `128-StockFlow/StockFlow.Web/.vscode/launch.json`
- `128-StockFlow/StockFlow.Web/.vscode/extensions.json`
- `128-StockFlow/StockFlow.Tests/ProductionTests.cs`
- `128-StockFlow/StockFlow.Tests/SchemaTests.cs`
- `128-StockFlow/StockFlow.Web/package.json`
- `128-StockFlow/StockFlow.Web/package-lock.json`
- `128-StockFlow/StockFlow.Web/ngsw-config.json`
- `128-StockFlow/StockFlow.Web/angular.json`
- `128-StockFlow/StockFlow.Tests/StockFlow.Tests.csproj`
- `128-StockFlow/StockFlow.Api/Data/DevelopmentSeeder.cs`
- `128-StockFlow/StockFlow.Api/Data/Entities.cs`
- `128-StockFlow/StockFlow.Web/README.md`
- `128-StockFlow/StockFlow.Web/proxy.conf.json`
- `128-StockFlow/StockFlow.Web/src/styles.scss`
- `128-StockFlow/StockFlow.Web/src/main.ts`
- `128-StockFlow/StockFlow.Web/src/index.html`
- `128-StockFlow/StockFlow.Api/Data/StockFlowDbContext.cs`
- `128-StockFlow/StockFlow.Api/Data/StockFlowDesignTimeFactory.cs`
- `128-StockFlow/StockFlow.Web/public/favicon.ico`
- `128-StockFlow/StockFlow.Web/public/manifest.webmanifest`
- `128-StockFlow/StockFlow.Tests/Infrastructure/StockFlowApiFactory.cs`
- `128-StockFlow/StockFlow.Web/src/app/dashboard.ts`
- `128-StockFlow/StockFlow.Api/Data/Migrations/StockFlowDbContextModelSnapshot.cs`
- `128-StockFlow/StockFlow.Api/Data/Migrations/20261001172033_InitialSchema.Designer.cs`
- `128-StockFlow/StockFlow.Api/Data/Migrations/20261001172033_InitialSchema.cs`
- `128-StockFlow/StockFlow.Web/src/app/app.routes.ts`
- `128-StockFlow/StockFlow.Web/src/app/app.config.ts`
- `128-StockFlow/StockFlow.Web/src/app/app.spec.ts`
- `128-StockFlow/StockFlow.Web/src/app/app.ts`
- `128-StockFlow/StockFlow.Web/src/app/auth/login.ts`
- `128-StockFlow/StockFlow.Web/src/app/auth/auth.service.ts`
- `128-StockFlow/StockFlow.Web/src/app/auth/auth.service.spec.ts`
- `128-StockFlow/StockFlow.Web/public/icons/icon.svg`

## Self review

All task1 acceptance items reviewed against audited brief. Generated migration is current (20261001172033_InitialSchema) with observed version and fingerprint; no SQLite/InMemory integration provider, no demo secrets in appsettings/package/source, and no production migrations on API startup. Added extended regression tests after initial RED coverage; no claim that each later regression assertion was separately RED first. No architectural requirement changes by this worker.

