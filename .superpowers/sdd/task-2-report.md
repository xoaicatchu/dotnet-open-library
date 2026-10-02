# StockFlow Task 2 implementation report

## Test-first RED

Command before implementation:

```text
dotnet test 128-StockFlow/StockFlow.slnx -c Release --filter "FullyQualifiedName~CatalogTests|FullyQualifiedName~ReceiptTests|FullyQualifiedName~SyncIdempotencyTests"
```

Fresh output: build succeeded; 7 selected integration tests failed against the missing Task 2 routes. The first assertion in each flow reached `404 Not Found` where product/supplier CRUD or receipt draft creation was expected. This confirms the tests exercise the API surface that is not implemented yet. Docker-backed PostgreSQL fixture started successfully.

## Implementation

Implemented admin-managed product/supplier catalogs, normalization before unique-index writes, paged reads, soft product deactivation, and a unit-change guard once journal entries exist. Warehouse/Admin can create and update receipt drafts; draft writes never touch balances. Posting claims the document with its expected version, applies bounded conditional balance updates, records one receipt movement per line, and commits the document/balance/journal within the same PostgreSQL transaction. Repeat post returns the already-posted state; stale edits and invalid states return stable 409 ProblemDetails.

The online and offline draft paths share `StockDocumentService`. Bounded `/api/sync` supports `CreateReceipt` and `UpdateReceipt`, binds each user-scoped `clientOperationId` to a canonical SHA-256 fingerprint, serializes concurrent retries with a transaction advisory lock, replays identical operations, and rejects changed payloads with 409. Unknown/malformed operations and batches over 50 operations or 1,000 total lines are rejected before writes. Npgsql retries transient failures at most three times with a two-second maximum retry delay; transaction retries rerun the entire command.

## Verification

Focused PostgreSQL integration tests after implementation:

```text
dotnet test 128-StockFlow/StockFlow.slnx -c Release --filter "FullyQualifiedName~CatalogTests|FullyQualifiedName~ReceiptTests|FullyQualifiedName~SyncIdempotencyTests"
Passed!  - Failed: 0, Passed: 10, Skipped: 0, Total: 10
```

Full solution test suite:

```text
dotnet test 128-StockFlow/StockFlow.slnx -c Release
Passed!  - Failed: 0, Passed: 22, Skipped: 0, Total: 22
```

Formatting diagnostics:

```text
dotnet format 128-StockFlow/StockFlow.slnx --verify-no-changes --no-restore --include <all Task 2 C# files and Program.cs>
exit code 0; no diagnostics
```

The failure-injection rollback test sets the second product balance version to its safe-integer ceiling, lets the first conditional update run, then confirms the second guarded update fails and the transaction leaves both balances, the journal, and document status unchanged. No transient network fault was injected into the execution strategy; its retry policy is configured in `Program.cs` and the full path is transaction-scoped.
