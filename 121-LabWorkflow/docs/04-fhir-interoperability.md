# 04. Hợp đồng FHIR và tích hợp

## 1. Phiên bản và quy tắc chung

- MVP nhắm FHIR R4, version 4.0.1, qua JSON với content type application/fhir+json.
- Chọn R4 cho reference implementation; trước khi tích hợp, đối chiếu phiên bản/Implementation Guide (IG) mà đối tác công bố. R5/R4B không được coi là package drop-in tương thích.
- Dùng Firely .NET SDK package Hl7.Fhir.R4. Chỉ thêm package validation tương ứng nếu kiểm tra profile được bật.
- Domain types, API DTO và FHIR POCO là ba mô hình khác nhau; ánh xạ nằm trong LabWorkflow.Fhir.
- FHIR resource/profile, terminology và endpoint capabilities phải version hóa, pin trong repo/config; không kéo “latest” profile mỗi lần chạy production.

FHIR Core xác định hình dạng resource và một phần semantics. IG của đối tác quyết định profile cụ thể, required fields, cardinality, code systems, search, transaction support và workflow tương thích.

## 2. Ánh xạ resource

| Mục đích | FHIR R4 | Trường/liên kết chính cần xét | Chủ sở hữu chính |
|---|---|---|---|
| Chỉ định từ EHR | ServiceRequest | status, intent, code, subject, requester, identifier, authoredOn, priority, occurrence/timing nếu có | EHR |
| Công việc cần làm | Task, tùy chọn | status, focus tới công việc được xử lý, for/owner/identifier tùy profile | Lab app hoặc EHR theo agreement |
| Mẫu vật lý | Specimen | identifier/accession, subject, type, collection, receivedTime, request tham chiếu ServiceRequest khi phù hợp | Lab app sau khi lấy/nhận; M1 gửi cùng report transaction |
| Một kết quả đo | Observation | status, code, subject, basedOn, specimen, effective[x], issued, value[x], unit, performer, referenceRange nếu được cung cấp có kiểm soát | Lab app sau khi nhập/duyệt |
| Báo cáo/nhóm kết quả | DiagnosticReport | status, code, subject, basedOn, specimen, result references, effective[x], issued, performer, conclusion nếu dùng | Lab app |
| Provenance nghiệp vụ | Provenance, nếu IG cần | target report/result; recorded; agents/activity/signature theo profile | Lab app |
| Audit trao đổi | AuditEvent, nếu đối tác yêu cầu | loại hành động, source, agent, entity, thời gian/kết quả | Mỗi hệ thống theo trách nhiệm của mình |

~~~mermaid
flowchart LR
    subgraph Internal["Domain nội bộ"]
        Order["IncomingOrder"] --> Case["LabCase"]
        Case --> Sample["Specimen"]
        Case --> Revision["ReportRevision snapshot"]
        Revision --> Component["ResultComponent"]
    end
    subgraph FHIR["FHIR R4 resources"]
        SR["ServiceRequest"]
        FSpec["Specimen"]
        DR["DiagnosticReport"]
        Obs["Observation"]
    end
    SR -. "nhập chỉ định" .-> Order
    Sample --> FSpec
    Revision --> DR
    Component --> Obs
    DR -->|"basedOn"| SR
    DR -->|"specimen"| FSpec
    DR -->|"result"| Obs
    Obs -->|"specimen"| FSpec
~~~

*Cách đọc:* các mũi tên trong khung Domain là dữ liệu workflow; các liên kết có nhãn là reference FHIR. M1 tạo một chuỗi DiagnosticReport cho mỗi LabCase và một Observation cho mỗi ResultComponent; LabCase không phải FHIR resource.

### ServiceRequest

Được xem là yêu cầu clinical service/test chứ không phải trạng thái của mẫu. Nếu chỉ định được tạo trước khi mẫu tồn tại, liên kết specimen được thêm khi specimen đã có; FHIR R4 nêu rõ cách dùng trường specimen phụ thuộc mẫu đã tồn tại ở thời điểm order hay chưa. Xác nhận profile/IG trước khi gửi.

### Specimen

Resource mô tả mẫu và sự kiện thu thập/nhận mẫu. Detailed operational states như ly tâm, phân aliquot hoặc di chuyển nội bộ thuộc workflow domain; không thêm các code tự tạo vào FHIR core status.

### Observation

Observation mô tả kết quả đơn lẻ hoặc nhóm thành phần theo profile. M1 chọn mapping đơn giản: mỗi ResultComponent thành đúng một Observation với code/value[x]/unit; các Observation được gom dưới một DiagnosticReport. Profile yêu cầu Observation có nhiều `component` hoặc grouping khác phải chuyển M2/reconciliation, không tự ép mapping.

### DiagnosticReport

DiagnosticReport gom ngữ cảnh/báo cáo và tham chiếu các Observation. FHIR có thể biểu diễn nhiều report, nhưng M1 giới hạn một ServiceRequest thành một LabCase, một DiagnosticReport ban đầu và một chuỗi correction revision; một report có thể tham chiếu nhiều Observation và Specimen. Không phát hành một phần hoặc nhiều report theo panel trong M1. Order compound hoặc không khớp chính xác một panel demo sẽ chuyển reconciliation; không tự tách case.

FHIR R4 khai báo `effective[x]` cardinality 0..1, nhưng phần mô tả Clinically Relevant Time (§10.3.5.2) yêu cầu DiagnosticReport SHALL luôn có trường này. Đây là thời điểm/thời khoảng có ý nghĩa lâm sàng của report: thường là lúc thực hiện procedure; có thể suy ra từ thời gian lấy specimen, nhưng không phải lúc nào cũng trùng (ví dụ xét nghiệm có định giờ). M1 lưu `TestExecutionTime` có căn cứ và ánh xạ sang `effectiveDateTime` hoặc `effectivePeriod`; tách khỏi `issued`, là thời điểm tạo phiên bản report. Thiếu TestExecutionTime thì chặn release; không tự điền giờ hiện tại hoặc dùng `issued` thay thế. Fixture tổng hợp phải có thời gian thực hiện tường minh. Xem [FHIR R4 DiagnosticReport](https://hl7.org/fhir/R4/diagnosticreport.html).

Trong M1, report đầu tiên phát hành status `final`; correction sau final tạo revision nội bộ mới và cập nhật cùng logical DiagnosticReport với status `corrected`. Observation có value thay đổi cũng được cập nhật cùng logical ID với status `corrected`; Observation không đổi giữ nguyên ID/version. Correction chỉ gửi report và các Observation đổi; giữ nguyên references tới Specimen đã gửi, không POST/PUT lại Specimen. Không dùng `amended`/`appended` trong M1. Contract M1 giữ business `identifier` ổn định theo LabCase/result component, lưu FHIR logical ID, versionId và ETag trả về từ lần gửi đầu trong `PublishedResourceLink`, rồi gửi `PUT` có `If-Match` cho revision correction. Transaction simulator phải tăng version và giữ bản trước để `vread`/history truy xuất được. Remote correction chỉ bật nếu CapabilityStatement công bố `versioned-update`, update/transaction cần thiết và history/vread theo contract; thiếu capability hoặc ETag thì chặn correction, chuyển NeedsAttention và không ghi đè vô điều kiện. Reason của correction giữ trong audit nội bộ; chỉ map ra FHIR nếu partner IG định nghĩa đúng trường/profile, không nhét vào `conclusion` hay extension tự tạo. Xem [FHIR R4 HTTP versioning](https://hl7.org/fhir/R4/http.html).

### Task

Task là tùy chọn. Dùng khi đối tác cần nhìn thấy workflow task; không tạo Task cho mọi thao tác nội bộ chỉ vì có state machine. Không thay Task status cho kết quả lâm sàng, cũng không dùng Task làm bản sao của ServiceRequest.

## 3. Luồng nhập chỉ định

### Remote mode

1. Gọi FHIR metadata endpoint /metadata và xác minh CapabilityStatement.
2. Tìm ServiceRequest theo các search interaction/search parameter mà server công bố. M1 có thể dùng polling; không giả định server hỗ trợ Subscription.
3. Khi dùng cursor _lastUpdated, lưu watermark bền vững, xử lý pagination link next, đọc chồng lấn một khoảng ngắn rồi dedupe để tránh bỏ sót resource cập nhật cùng timestamp.
4. Parse Bundle bằng Firely, xác thực core structure và partner profile.
5. Tìm một IncomingOrder theo source key + FHIR base URL + resource type/id; dedupe phiên bản bằng meta.versionId, hoặc canonical content hash khi versionId vắng mặt.
6. Lưu ImportedOrderVersion rồi cập nhật cùng IncomingOrder/LabCase; version mới sau khi workflow bắt đầu tạo reconciliation, không tạo LabCase mới. Không lưu FHIR JSON nguyên bản trong log.
7. Ghi checkpoint chỉ sau khi batch import được lưu thành công. Batch lỗi một resource phải được ghi IntegrationIssue; chính sách checkpoint không được làm các resource sau cursor bị bỏ qua.

Report `basedOn`/subject phải tham chiếu resource mà destination có thể phân giải. M1 remote giả định order được import từ chính server sẽ nhận report; nếu source/destination khác nhau, phải có mapping reference/identifier mà partner chấp thuận hoặc dừng ở reconciliation, không gửi reference mồ côi.

Nếu partner không hỗ trợ search parameter/capability cần thiết, connector phải báo rõ không tương thích và ngừng import an toàn; không giả định query sẽ được server hỗ trợ.

### Demo mode

- Fixture JSON là FHIR R4 synthetic và có thể parse/serialize bằng Firely SDK.
- Demo adapter có order catalog cố định, import idempotent và FHIR simulator ghi bundle nhận được để người học xem.
- Demo phải sử dụng cùng mapping/validation path như remote mode; chỉ thay transport/identity.
- Fixture code systems dùng namespace synthetic; không thể xem là mã xét nghiệm thật.

## 4. Luồng gửi kết quả

1. Lấy ReportRevision bất biến cùng snapshot của các specimen được chấp nhận và kết quả được chọn.
2. Đọc `PublishedResourceLink` theo destination. Lần gửi đầu dùng create và lưu logical ID, versionId, ETag từ transaction-response; correction bắt buộc reuse logical ID/business identifier đã lưu, không tạo định danh mới theo revision.
3. Sinh Specimen resources từ snapshot; M1 không gửi các trạng thái nội bộ như đang vận chuyển/ly tâm.
4. Sinh đúng một Observation cho mỗi ResultComponent theo mapping M1 đã cấu hình.
5. Lần đầu tạo đúng một DiagnosticReport tham chiếu ServiceRequest, Specimen và toàn bộ Observation; status `final`. Correction cập nhật cùng DiagnosticReport logical ID với status `corrected`, và chỉ PUT Observation có nội dung đổi (cũng status `corrected`) bằng ETag hiện tại; M1 không phát hành partial report.
6. Ánh xạ `TestExecutionTime` của ResultSet sang `DiagnosticReport.effective[x]` và sang từng `Observation.effective[x]` trong panel M1; ánh xạ thời điểm phát hành do server ghi nhận sang `issued`. Không thay thế effective bằng specimen collection hoặc issued nếu chúng không có cùng ý nghĩa.
7. Dùng urn:uuid fullUrl trong transaction Bundle lần đầu khi resource tạo mới cần tham chiếu lẫn nhau; correction dùng logical references đã lưu và Bundle request `PUT` có `ifMatch` cho từng resource bị cập nhật.
8. Validate cấu trúc FHIR R4; remote mode thêm profile/terminology validation theo IG đã pin.
9. Gửi transaction vào FHIR base URL chỉ khi CapabilityStatement cho biết transaction được hỗ trợ.
10. Đọc transaction-response từng entry: HTTP status, location, ETag và `meta.versionId` nếu có. Lưu PublishedResourceLink để các correction sau nhắm đúng resource/version.
11. Chỉ cập nhật OutboxMessage thành Delivered khi toàn bộ transaction thành công theo semantics của FHIR server.

FHIR transaction xử lý một tập thao tác trên một FHIR server theo nguyên tắc thành công toàn bộ hoặc từ chối toàn bộ. Nó không tạo distributed transaction giữa PostgreSQL của ứng dụng và FHIR server; cũng không chứng minh rằng mọi server partner xử lý mọi profile/field theo một cách giống nhau. Đọc CapabilityStatement và contract-test với server đích.

## 5. Idempotency, conflict và version

~~~mermaid
sequenceDiagram
    autonumber
    participant App as API / Application
    participant DB as PostgreSQL
    participant Worker as Outbox Worker
    participant FHIR as FHIR R4 Server / Simulator

    App->>DB: Commit ReportRevision + AuditEntry + OutboxMessage
    Worker->>DB: Claim message; read revision + PublishedResourceLinks
    alt Lần phát hành đầu
        Worker->>FHIR: POST transaction: Specimen + Observations + DiagnosticReport final
        alt 2xx
            FHIR-->>Worker: Logical IDs / locations / ETags
            Worker->>DB: Save links; mark Delivered
        else Timeout / kết quả chưa rõ
            Worker->>FHIR: Lookup theo stable identifiers; đối soát
            FHIR-->>Worker: Resource hiện hành và version
            Worker->>DB: Chỉ mark Delivered nếu khớp chính xác revision
        end
    else Correction
        Note over Worker,DB: Outbox giữ IDs và ETags kỳ vọng lúc correction được tạo
        Worker->>FHIR: PUT DiagnosticReport + Observations đổi, If-Match
        Note over Worker,FHIR: Không tạo lại hoặc cập nhật Specimen trong correction
        alt 2xx
            FHIR-->>Worker: Version / ETag mới
            Worker->>DB: Update links; mark Delivered
        else HTTP 412
            FHIR-->>Worker: Precondition failed
            Worker->>FHIR: Đọc resource/history; không retry với ETag mới mù quáng
            FHIR-->>Worker: Resource hiện hành và version
            Worker->>DB: Chỉ mark Delivered nếu khớp chính xác; còn lại NeedsAttention
        else Timeout / kết quả chưa rõ
            Worker->>FHIR: Đọc resource/history theo PublishedResourceLink
            FHIR-->>Worker: Resource hiện hành và version
            Worker->>DB: Chỉ mark Delivered nếu khớp chính xác; còn lại reconciliation
        end
    end
~~~

*Cách đọc:* lần gửi đầu tạo tài nguyên trong transaction; correction cập nhật cùng logical IDs với điều kiện ETag và chỉ gửi Observation thay đổi. Timeout hoặc HTTP 412 đi qua bước đọc/đối soát, không tự ghi đè phiên bản mới hơn. Chi tiết capability bắt buộc nằm ở các mục dưới đây.

- Mọi import/outbound revision có stable key duy nhất.
- Ưu tiên conditional create/update chỉ khi CapabilityStatement xác nhận server hỗ trợ đúng interaction và search key.
- Nếu remote POST timeout, kết quả có thể là “server đã commit nhưng client chưa nhận response”. Trước khi retry cần tìm theo identifier/đối soát; không tạo ID mới vô điều kiện.
- Nếu correction transaction timeout rồi lần retry nhận 412, đọc DiagnosticReport/Observations theo PublishedResourceLink và reconcile với ReportRevision đang gửi. Chỉ đánh dấu Delivered/cập nhật ETag nếu mọi resource hiện tại khớp chính xác correction đó; nếu không khớp thì NeedsAttention, không ghi đè phiên bản mới hơn.
- Nếu server hỗ trợ versioning/ETag, dùng version-aware update (If-Match) để chặn lost update. Không update resource nguồn do EHR sở hữu trừ khi hợp đồng tích hợp cho phép.
- M1 correction yêu cầu CapabilityStatement versioning `versioned-update`; thiếu ETag hoặc nhận HTTP 412 thì không retry bằng version mới một cách mù quáng, cần đọc resource/history rồi reconciliation.
- Lưu transaction response location/version theo từng resource; không giả định server giữ resource id do client gửi.
- Lưu business key ↔ FHIR logical ID ↔ latest versionId/ETag theo từng destination; không dùng ReportRevisionId nội bộ làm FHIR logical ID mới qua mỗi correction.
- Khi server đã nhận một số resource nhưng response không nhất quán với transaction expectation, giữ Outbox ở NeedsAttention và có quy trình reconciliation.
- Idempotency key nội bộ không mặc định được server FHIR hiểu; remote behavior phụ thuộc conditional interaction và identifier support.

## 6. Profile validation và terminology

- M1 dùng Firely structural validation để phát hiện lỗi FHIR model/cardinality/type; các fixture dùng mã tổng hợp và không được xem là partner-conformant.
- Remote mode dùng profile validation với StructureDefinition/IG đúng version và terminology resolver tương ứng.
- Ghim package/version của partner IG trong fixture/config và ghi rõ profile canonical URL.
- Lỗi validation trả OperationOutcome nội bộ có issue codes/path; UI operator nhận thông báo an toàn, log chỉ ghi mã lỗi/correlation id.
- LOINC thường dùng cho mã xét nghiệm/Observation; xác minh từng mã và nội dung đo.
- UCUM dùng cho unit codes khi quy định profile đòi hỏi.
- Một profile hợp lệ không chứng minh rằng kết quả hay quy trình xét nghiệm đúng về mặt lâm sàng.

## 7. Capability preflight

Remote connector phải có health/readiness status riêng. Preflight cần xác nhận:

- server base URL HTTPS, TLS và FHIR version phù hợp R4 4.0.1;
- metadata/CapabilityStatement đọc được;
- resource types ServiceRequest, Specimen, Observation, DiagnosticReport được công bố cho interaction cần dùng;
- versioned-update/ETag và vread/history được hỗ trợ nếu bật correction remote;
- search parameter cần cho import được hỗ trợ;
- transaction được hỗ trợ nếu publish bundle atomic được yêu cầu;
- format JSON và profile/version mà adapter cần được server chấp nhận;
- authorization endpoint/token behavior/scope đã cấu hình.

Thiếu một khả năng bắt buộc thì connector báo degraded/blocked và giữ message pending. Không downgrade âm thầm từ transaction sang batch hoặc nhiều POST độc lập.

## 8. Authorization

- Nhân viên ứng dụng: dùng OIDC/SMART launch do môi trường đích hỗ trợ; role/policy nội bộ vẫn kiểm tra tại API.
- Worker không có user tương tác: dùng SMART Backend Services hoặc cơ chế OAuth/security đã được đối tác phê duyệt; nếu dùng SMART, ưu tiên xác thực asymmetric private-key JWT và scope tối thiểu.
- Không dùng JWT do project 108 tự cấp làm token FHIR trừ khi FHIR server đã cấu hình tin cậy issuer/audience đó.
- Lưu scope/credential mapping theo từng environment. Demo mode không gọi FHIR server bên ngoài.

## 9. Nguồn chuẩn

- [FHIR R4 ServiceRequest](https://hl7.org/fhir/R4/servicerequest.html)
- [FHIR R4 Task](https://hl7.org/fhir/R4/task.html)
- [FHIR R4 Specimen](https://hl7.org/fhir/R4/specimen.html)
- [FHIR R4 Observation](https://hl7.org/fhir/R4/observation.html)
- [FHIR R4 DiagnosticReport](https://hl7.org/fhir/R4/diagnosticreport.html)
- [FHIR R4 DiagnosticReport status](https://hl7.org/fhir/R4/valueset-diagnostic-report-status.html), [Observation status](https://hl7.org/fhir/R4/valueset-observation-status.html)
- [FHIR R4 CapabilityStatement](https://hl7.org/fhir/R4/capabilitystatement.html), [Bundle](https://hl7.org/fhir/R4/bundle.html), [HTTP/transactions](https://hl7.org/fhir/R4/http.html)
- [FHIR R4 AuditEvent](https://hl7.org/fhir/R4/auditevent.html), [Provenance](https://hl7.org/fhir/R4/provenance.html)
- [Firely SDK 6 overview](https://docs.fire.ly/projects/Firely-NET-SDK/en/sdk6/), [R4 package selection](https://docs.fire.ly/projects/Firely-NET-SDK/en/sdk6/getting-started.html), [transactions](https://docs.fire.ly/projects/Firely-NET-SDK/en/sdk6/client/transactions.html), [validation](https://docs.fire.ly/projects/Firely-NET-SDK/en/latest/validation.html)
- [SMART App Launch 2.2](https://hl7.org/fhir/smart-app-launch/), [App Launch](https://hl7.org/fhir/smart-app-launch/app-launch.html), [Backend Services](https://hl7.org/fhir/smart-app-launch/backend-services.html), [scopes](https://hl7.org/fhir/smart-app-launch/scopes-and-launch-context.html)
- [LOINC User Guide](https://loinc.org/kb/users-guide/introduction/), [UCUM](https://ucum.org/ucum)

