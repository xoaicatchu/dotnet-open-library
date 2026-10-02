# StockFlow execution progress

Baseline commit before implementation: `29605b7ac4f694a2430c0e6fd86d0b7b7a370037`.

Scope: new `128-StockFlow/` project only, plus the planned additive root README entry after implementation. The checkout already has unrelated uncommitted work; do not stage, commit, revert, or edit it. Do not touch `127-WASM-WASI/`.

Environment verified before Task 1: .NET SDK 10.0.100/10.0.401 available; Node 22.22.1/npm 10.9.4; Docker server 29.8.0; API ports 5166/5167 and Angular 4200 free. Angular 21 LTS is selected because its official support range includes Node 22.22.1; Angular 22 needs a later Node patch.

Plan corrections before implementation and confirmed in an independent plan audit: shared ASP.NET Data Protection keys persist in PostgreSQL and are encrypted at rest by a certificate mounted from a Kubernetes Secret, so auth cookies work across stateless replicas. The production first Admin is provisioned with a one-shot `--bootstrap-admin` command. Task 1 tests cover auth/schema/bootstrap; product authorization and duplicate SKU move to Task 2 because the products endpoint does not exist in Task 1. V1 has bounded draft push and REST pagination, no global change-feed cursor; operation IDs bind to canonical request hashes; offline stocktake retains the observed balance version; reports read all aggregates in one SQL statement; Angular uses the memory-token CSRF contract.

## Tasks

- Task 1 — Solution, database and authentication: APPROVED (implementer `/root/stockflow_task_1`; code review `/root/stockflow_task_1_review`, 0 issues). Evidence: `.superpowers/sdd/task-1-report.md`; reviewer independently reran .NET 12/12 and Angular 5/5 and production smoke checks.
- Task 2 — Catalog, receipts, journal and idempotent sync: IMPLEMENTED, verification GREEN (implementer `/root/stockflow_task_2`); independent review IN PROGRESS (`/root/stockflow_task_2_review`). Evidence: `.superpowers/sdd/task-2-report.md`.
- Task 3 — Issues, reversals and concurrent posting: pending.
- Task 4 — Stocktakes and stock reports: pending.
- Task 5 — Angular offline-capable UI and browser E2E: pending.
- Task 6 — README, HTTP examples, Docker/Kubernetes manifests, load and deployment verification: pending.

