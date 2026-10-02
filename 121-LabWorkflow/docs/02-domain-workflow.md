# 02. Mô hình miền và quy tắc nghiệp vụ

## 1. Ranh giới domain

Domain nội bộ quản lý **quy trình công việc** và các phiên bản nhập/duyệt. FHIR adapter quản lý việc ánh xạ ra resource chuẩn. Không dùng Firely POCO trực tiếp làm aggregate, EF entity hoặc DTO nội bộ.

Một LabCase đại diện cho việc phòng xét nghiệm xử lý đúng một upstream ServiceRequest. Một case có thể có nhiều specimen và nhiều Observation/result component. Trong M1, một case có đúng một chuỗi DiagnosticReport: một báo cáo ban đầu gom toàn bộ kết quả rồi các revision correction; không có multi-report, panel phát hành riêng hoặc partial release. Order không khớp đúng một panel demo chuyển reconciliation; M1 không tự tách case. Đây là giới hạn có chủ ý của showcase, không phải giới hạn của FHIR.

## 2. Mô hình khái niệm

~~~mermaid
erDiagram
    INCOMING_ORDER ||--|| LAB_CASE : creates
    INCOMING_ORDER ||--o{ IMPORTED_ORDER_VERSION : observes
    LAB_CASE ||--o{ SPECIMEN : has
    LAB_CASE ||--o{ RESULT_SET : records
    RESULT_SET ||--|{ RESULT_COMPONENT : contains
    LAB_CASE ||--o{ REPORT_REVISION : issues
    REPORT_REVISION ||--o{ CORRECTION_DRAFT : corrected_from
    REPORT_REVISION ||--o{ OUTBOX_MESSAGE : publishes
    LAB_CASE ||--o{ AUDIT_ENTRY : records
    LAB_CASE ||--o{ INTEGRATION_ISSUE : reconciles
~~~

### IncomingOrder

- Stable internal identity for one upstream ServiceRequest, unique by source system + FHIR base URL + resource type + resource id; never create a second IncomingOrder/LabCase just because version changes.
- Source system key + FHIR base URL key.
- `AcceptedImportedOrderVersionId` là con trỏ chuẩn duy nhất của snapshot nghiệp vụ đang được chấp nhận; `LatestObservedVersionId` trỏ tới version nguồn mới nhất đã nhập.
- Khi thêm version mới, ghi version row và cập nhật latest pointer trong cùng transaction. Chỉ dịch accepted pointer trước khi có lab action/specimen/result; việc đổi pointer và audit event phải commit cùng transaction.
- No complete Patient record; only the external reference and a minimal synthetic display snapshot in demo.

### ImportedOrderVersion

- Belongs to one stable IncomingOrder; unique by IncomingOrder ID + observed version key.
- Uses source `meta.versionId` as the version key when present; otherwise uses SHA-256 over a deterministic canonical representation of the parsed R4 resource content. Store `meta.lastUpdated` separately for ordering/checkpoint purposes.
- Imported time, source `meta.lastUpdated` and content fingerprint are stored separately; accepting a new version records an audit event with previous/new version keys and the importing machine identity.
- Stores the normalized source snapshot needed for audit/reconciliation: status, intent, code, subject reference, identifiers, requester, priority and timing. Apply data access/retention controls; do not log raw FHIR JSON.
- Duplicate import of the same version key is a no-op. New version adds a row and updates `LatestObservedVersionId`; it never creates a new LabCase.
- If no lab action has been accepted and no specimen/result work has started, the canonical accepted pointer on IncomingOrder may move to the new row. After work starts, keep it stable and create ReconciliationRequired + IntegrationIssue referencing old/new version rows in the same transaction that records the conflict.

~~~mermaid
flowchart TD
    A["Nguồn gửi ServiceRequest version"] --> B{"Version key đã tồn tại?"}
    B -- "Có" --> C["No-op idempotent; giữ nguyên case"]
    B -- "Không" --> D["Ghi ImportedOrderVersion mới<br/>cập nhật LatestObservedVersionId"]
    D --> E{"Lab đã claim / nhận việc,<br/>tạo specimen hoặc nhập result?"}
    E -- "Chưa" --> F["Chuyển AcceptedImportedOrderVersionId<br/>và ghi audit cùng transaction"]
    E -- "Rồi" --> G["Giữ accepted version đang dùng"]
    G --> H["Tạo IntegrationIssue / ReconciliationRequired<br/>tham chiếu version cũ và mới"]
    C --> I["Một IncomingOrder + một LabCase"]
    F --> I
    H --> I
~~~

*Cách đọc:* mỗi phiên bản nguồn được lưu dưới cùng IncomingOrder và LabCase. Chỉ được chuyển version đang chấp nhận trước khi workflow bắt đầu; nếu đã có thao tác xét nghiệm thì giữ snapshot hiện hành và yêu cầu đối soát.

### LabCase

- Internal GUID.
- IncomingOrder ID.
- Does not store a second accepted-version pointer. The working order snapshot is resolved through IncomingOrder.AcceptedImportedOrderVersionId, so there is only one source of truth.
- Local operational state/version.
- Created/updated timestamp.
- Optional assignment/claim identity.
- Does not duplicate all FHIR fields. Keeps external reference and necessary immutable workflow snapshot.

### Specimen

- Internal GUID, LabCase ID, unique accession identifier.
- Coded type and optional container data.
- Collected/received times and actor references.
- Internal operational state and rejection reason code.
- External Specimen resource id/version after successful publication, if applicable.

### TestDefinition

- Locally configured order/analyte identifiers and code-system URI.
- Required specimen types.
- Result kind: numeric, coded choice or text.
- Required unit/code system for each supported result type.
- Required/optional result components for demo panels.
- These are demo/reference definitions, not clinically approved catalogs.

### ResultSet / ResultComponent

- ResultSet points to LabCase and selected specimen(s).
- ResultSet stores the panel's `TestExecutionTime` as an instant or period shared by its components in M1; it is separate from server-generated `enteredAt`.
- ResultComponent includes the coded analyte, value type/value, unit coding where required, result status, entered-by actor and timestamp.
- Quantitative values use a decimal representation and explicit unit; coded values carry system + code + display.
- Before release, result values can be corrected with audit. After release, create a report revision; do not overwrite released snapshot.

### ReportRevision

- LabCase ID, revision number, status, author/reviewer, reason and issued time.
- `TestExecutionTime` tách biệt `IssuedAt`: thời điểm/thời khoảng có căn cứ khi xét nghiệm được thực hiện để ánh xạ `DiagnosticReport.effective[x]`; không suy ra từ thời điểm phát hành.
- Immutable released snapshot of the selected result components and specimen references.
- Correction keeps the report's clinically relevant `TestExecutionTime` unless a correction draft explicitly changes it with a reason.
- Optional previous revision reference.
- Stable idempotency key for publishing.
- M1 tạo đúng một logical DiagnosticReport cho report chain. Bản đầu có status `final`; correction tạo ReportRevision mới và gửi version-aware update với FHIR status `corrected`. `amended`/`appended` và nhiều DiagnosticReport cho một case ngoài M1.

### CorrectionDraft

- LabCase ID và `BaseReportRevisionId`, bắt buộc trỏ tới ReportRevision mới nhất.
- Full corrected result snapshot, TestExecutionTime, required reason code/text, author và created time.
- Draft được submit thẳng sang `AwaitingReview`; không ghi đè ReportRevision và không được đưa vào FHIR/outbox.
- LabCase vẫn ở trạng thái `Released`; trạng thái report đã phát hành và delivery không đổi trong lúc correction chờ duyệt. `CorrectionPendingReview` là trạng thái riêng của draft/worklist badge.
- Reviewer khác author mới có thể release. Release chuyển snapshot thành ReportRevision kind `Correction`, lưu audit và outbox nguyên tử.
- Chỉ một draft active cho mỗi LabCase; nếu cần sửa draft, hủy draft cũ và tạo draft mới. Draft cũ được giữ audit trail.
- Không mở correction khi report gần nhất chưa Delivered hoặc chưa hoàn tất reconciliation; không xếp hai update FHIR version phụ thuộc nhau cùng lúc.

~~~mermaid
flowchart LR
    A["ReportRevision mới nhất"] --> B{"Đã Delivered / đối soát?"}
    B -- "Chưa" --> X["Chặn tạo correction"]
    B -- "Rồi" --> C["LabTechnician tạo full-snapshot draft<br/>base revision + expected case version + reason"]
    C --> D{"Draft active khác<br/>đã tồn tại?"}
    D -- "Có" --> Y["Chặn tạo draft thứ hai;<br/>hủy draft cũ có audit trước khi tạo mới"]
    D -- "Không" --> E["AwaitingReview; báo cáo đã phát hành<br/>và outbox giữ nguyên"]
    E --> F{"Reviewer khác author<br/>và base/version còn mới?"}
    F -- "Không" --> Z["Chặn release; cần xử lý draft/version"]
    F -- "Có" --> G["Một DB transaction:<br/>Correction ReportRevision + AuditEntry + OutboxMessage"]
    G --> H["Worker gửi cập nhật FHIR<br/>version-aware"]
~~~

*Cách đọc:* draft chỉ là đề xuất sửa; nó chưa thay đổi báo cáo đã phát hành và chưa tạo message FHIR. Chỉ bước release của Reviewer đủ điều kiện mới tạo revision correction cùng audit và outbox.

### PublishedResourceLink (integration persistence)

- Map destination + local business key/resource type sang FHIR logical ID, stable business Identifier, latest versionId/ETag và last successful publication time.
- `DiagnosticReport` có một link ổn định trên mỗi LabCase; Observation link ổn định theo result component key. Không tạo FHIR logical ID mới cho mỗi ReportRevision.
- Worker cập nhật link chỉ sau transaction-response thành công; correction outbox chụp version/ETag kỳ vọng tại lúc tạo để phát hiện remote drift.
- Dữ liệu link là integration metadata, không chứa token hoặc duplicate clinical payload; persistence không dùng Firely POCO.

### OutboxMessage

- Message ID, report revision ID, destination key, stable idempotency key.
- State, attempt count, next-attempt time, claim/lease time, last sanitized error code, delivered time.
- Unique constraint over the chosen destination + report revision/idempotency key.
- Do not log a serialized clinical Bundle. Keep payload access restricted if a payload snapshot is persisted.
- Initial M1 publication sends the immutable ReportRevision plus accepted Specimen and one Observation per ResultComponent. A correction message sends the corrected DiagnosticReport and changed Observation resources only, referencing the previously published Specimen/Observation logical IDs; it never recreates or rewrites specimens. Intermediate collection/processing statuses stay local.
- Correction outbox snapshot includes the expected remote logical IDs and ETags from PublishedResourceLinks. Do not refresh these preconditions during an automatic retry; a stale ETag requires reconciliation.

### AuditEntry

- Actor/user or machine identity, action code, entity reference, timestamp, reason, correlation/trace ID and outcome.
- Store only the minimum before/after values needed for a defensible workflow history. Do not copy full clinical payloads into ordinary logs.
- Clinical revision history and security access audit are separate concerns.

## 3. Ba vòng đời độc lập

### 3.1 Chỉ định / case

~~~mermaid
stateDiagram-v2
    [*] --> Received
    Received --> AwaitingSpecimen: valid active order
    Received --> ReconciliationRequired: unsupported or inconsistent source data
    AwaitingSpecimen --> InProgress: accepted specimen
    AwaitingSpecimen --> Cancelled: cancellation before work
    InProgress --> AwaitingReview: complete result set
    InProgress --> ReconciliationRequired: upstream conflict
    AwaitingReview --> ReconciliationRequired: upstream conflict after work started
    AwaitingReview --> InProgress: reviewer returns for correction
    AwaitingReview --> Released: reviewer releases
    Released --> Released: later report revision
    Released --> ReconciliationRequired: upstream update after release
    ReconciliationRequired --> AwaitingSpecimen: resolved before specimen work
    ReconciliationRequired --> InProgress: authorized resolution; resume saved specimen/result progress
    ReconciliationRequired --> AwaitingReview: resolve; resume saved review state
    ReconciliationRequired --> Released: resolve; previous released report remains published
    ReconciliationRequired --> Cancelled: authorized resolution
~~~

Case status is an internal summary. It is not copied into an arbitrary FHIR resource status. Reconciliation saves the prior operational state and resumes it after an authorized resolution; released report history and delivery status remain separate.

### 3.2 Specimen

| Trạng thái nội bộ | Chuyển tiếp hợp lệ |
|---|---|
| Expected | Collected, Cancelled |
| Collected | Received, Rejected |
| Received | Accepted, Rejected |
| Accepted | Processing, Cancelled only when local SOP permits |
| Processing | Processed, Rejected/Invalidated with reason |
| Processed | Remains historical; downstream result/report work continues |
| Rejected | Terminal for that physical sample; a replacement specimen is a new record |
| Cancelled | Terminal |

Các trạng thái này là workflow nội bộ. FHIR Specimen.status chỉ dùng giá trị chuẩn được định nghĩa trong FHIR release/profile; không tạo code tùy ý như in-transit/centrifuged trong FHIR core field.

### 3.3 Result/report and delivery

| Aggregate | Trạng thái | Ý nghĩa |
|---|---|---|
| Result component | Draft | Được nhập/sửa nhưng chưa phát hành. |
| Result component | ReadyForReview | Đủ thành phần để Reviewer xem xét. |
| Report revision | Final | Reviewer đã phát hành snapshot bất biến. |
| Report revision | Correction | Một revision mới sửa lỗi nội dung đã phát hành; giữ revision trước và ánh xạ FHIR status `corrected`. |
| Report revision | Cancelled/EnteredInError | Chỉ dùng khi chính sách/profile của đối tác cho phép; có reason và audit. |
| Correction draft | AwaitingReview | Kỹ thuật viên đã gửi snapshot sửa đầy đủ; chưa phải báo cáo lâm sàng mới và chưa được gửi ra FHIR. |
| Correction draft | Released/Cancelled | Reviewer tạo ReportRevision correction, hoặc draft bị hủy có lý do. |
| Outbox message | Pending | Báo cáo đã phát hành nội bộ, còn chờ gửi. |
| Outbox message | InProgress | Worker đã claim có lease. |
| Outbox message | RetryWaiting | Lỗi tạm thời; sẽ gửi lại vào thời điểm đã lên lịch. |
| Outbox message | Delivered | FHIR server trả phản hồi thành công và được lưu. |
| Outbox message | NeedsAttention | Lỗi cấu hình/dữ liệu hoặc hết số lần retry tự động; cần người xử lý. |

**Released không đồng nghĩa Delivered.** Tình trạng lâm sàng của báo cáo và tình trạng giao nhận tích hợp là hai chiều riêng.

## 4. Invariants — điều kiện không được phá vỡ

1. Một danh tính ServiceRequest chỉ có một IncomingOrder và một LabCase; mỗi observed version chỉ có một ImportedOrderVersion theo nguồn + FHIR base URL + resource type + resource id + version key.
2. IncomingOrder.AcceptedImportedOrderVersionId là nguồn chuẩn duy nhất cho accepted snapshot; LabCase không lưu con trỏ bản sao. Thay accepted pointer và audit event, hoặc ghi nhận conflict + IntegrationIssue, phải nguyên tử trong cùng transaction PostgreSQL.
3. Không chấp nhận chỉ định đã hủy để tạo công việc mới; update/cancel đến sau khi xử lý bắt đầu phải tạo reconciliation issue. Mọi lần đổi accepted version trước khi xử lý cũng được audit.
4. Accession identifier duy nhất trong phạm vi lab và không tái sử dụng sau khi specimen bị từ chối/hủy.
5. Chỉ specimen ở trạng thái được chấp nhận mới được gắn với kết quả được gửi duyệt.
6. Mọi component bắt buộc của panel phải có kết quả hợp lệ trước khi chuyển report sang review.
7. Kết quả số phải có kiểu số, đơn vị và mã analyte đúng cấu hình; không tự tính/đổi đơn vị nếu chưa có quy tắc được phê duyệt.
8. Chỉ người có policy Reviewer mới được phát hành; actor, timestamp và revision được lưu trong cùng DB transaction.
9. Kết quả đã phát hành bất biến; correction tạo revision mới có reason và liên kết revision trước.
10. CorrectionDraft phải dựa trên ReportRevision mới nhất đã Delivered/đối soát; chỉ một draft active/case và Reviewer phải khác author.
11. CorrectionDraft chứa full result snapshot, base ReportRevisionId, expected LabCase aggregate version, TestExecutionTime và reason; stale base/version không thể phát hành.
12. Tạo report release/correction revision, audit entry và outbox message là một transaction PostgreSQL.
13. Một revision không được đánh dấu Delivered nếu chưa xử lý transaction-response thành công hoặc chưa qua đối soát.
14. Mọi command update dùng optimistic concurrency version; stale update trả conflict, không ghi đè.
15. Audit log, application log và FHIR payload có phân quyền/lưu giữ riêng; mặc định không log PHI.

## 5. Luật chuyển trạng thái và ngoại lệ

- Mỗi transition có actor, thời điểm, lý do nếu chuyển sang Rejected/Cancelled/Correction/NeedsAttention.
- Transition không hợp lệ trả mã domain error ổn định; Controller chuyển thành ProblemDetails.
- Upstream đổi mã test sau khi đã lấy mẫu: giữ snapshot order ban đầu và tạo issue cần operator giải quyết.
- Upstream gửi lại cùng resource version key: trả no-op thành công; không nhân đôi version record, IncomingOrder, LabCase hoặc specimen.
- Upstream version mới trước khi lab nhận việc/bắt đầu xử lý: lưu version history và có thể dịch IncomingOrder.AcceptedImportedOrderVersionId trên cùng case trong cùng transaction với audit. Nếu đã có lab action/specimen/result, giữ accepted pointer và chuyển ReconciliationRequired; chỉ thay pointer sau quyết định có audit. Trường non-clinical chỉ được cập nhật nếu nằm trong whitelist đã cấu hình.
- Worker nhận response không rõ kết quả do timeout sau POST: không kết luận thất bại và tạo bundle mới với danh tính mới. Thực hiện lookup/conditional create theo capability rồi đối soát bằng stable identifier.
- Correction sau final: ghi revision nội bộ mới, giữ nguyên snapshot/audit cũ và gửi `corrected` qua version-aware update trong cùng logical FHIR report chain. Partner-specific identifier/ETag behavior phải theo IG và contract test; không giả định tạo resource id mới là tương đương correction.

## 6. Mã hóa và terminology

- Mọi coding gồm system URI + code + display; display chỉ để người đọc, không dùng làm identity.
- Mã xét nghiệm/analyte dùng code system mà partner IG quy định. LOINC là ứng viên thường dùng cho mã quan sát xét nghiệm, không tự gán code khi chưa xác minh đúng nội dung đo.
- Đơn vị số dùng UCUM code khi profile/đối tác yêu cầu; không coi chuỗi hiển thị mg/dL là đủ nếu cần trao đổi máy.
- Câu trả lời categorical dùng ValueSet/code system đã thống nhất; không tự phát minh mã lâm sàng.
- Demo dùng namespace/code fixture giả được đánh dấu synthetic; không nhầm lẫn với thuật ngữ thật.
- Reference range, interpretation và panic/critical flags không được tính tự động. Nếu sau này thêm, phải lấy tiêu chí đã được đơn vị chuyên môn phê duyệt và version hóa.

## 7. Định hướng chất lượng nghiệp vụ

ISO 15189:2022 tập trung vào chất lượng và năng lực của phòng xét nghiệm. Project có thể hỗ trợ traceability, quyền duyệt và lịch sử sửa đổi, nhưng các tài liệu này không mô tả toàn bộ QMS, xác nhận năng lực, kiểm soát thiết bị, nội kiểm/ngoại kiểm hay SOP. Đơn vị chuyên môn phải xác định những nội dung này trước khi dùng thật.

