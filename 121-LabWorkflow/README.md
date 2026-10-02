# 121-LabWorkflow

> **Bộ đặc tả và kế hoạch triển khai hệ thống điều phối xét nghiệm có trao đổi FHIR R4**

**Trạng thái hiện tại:** thư mục này là bộ tài liệu đặc tả để giao cho Gemini triển khai. Chưa có source code, database, API chạy được hoặc kết quả kiểm thử. Không dùng tài liệu này để khẳng định hệ thống đã sẵn sàng cho bệnh viện hay đã đáp ứng tiêu chuẩn/chứng nhận nào.

## Bài toán

Một phòng xét nghiệm nhận chỉ định từ EHR, theo dõi mẫu từ lúc lấy đến lúc tiếp nhận, nhập và duyệt kết quả, sau đó gửi báo cáo có cấu trúc về FHIR server của bệnh viện.

Ứng dụng là lớp điều phối và tích hợp. Nó không thay thế EHR hoặc toàn bộ LIS, không tự diễn giải lâm sàng, không điều khiển máy xét nghiệm và không phát minh mã xét nghiệm hay ngưỡng tham chiếu.

## Nhìn nhanh hệ thống

Luồng nghiệp vụ chính: nhận chỉ định → xử lý mẫu và kết quả → người có quyền duyệt → phát hành báo cáo → chuyển tới FHIR. Bản đầu gộp kết quả vào một report chain cho mỗi case.

~~~mermaid
flowchart LR
    EHR["EHR / FHIR server"] -->|ServiceRequest| IN["Nhập chỉ định<br/>IncomingOrder + version history"]
    IN --> CASE["LabCase"]
    CASE --> SPEC["Tiếp nhận specimen"]
    SPEC --> RESULT["Nhập kết quả"]
    RESULT --> REVIEW["Reviewer duyệt"]
    REVIEW --> REPORT["ReportRevision bất biến"]
    REPORT --> OUTBOX["Outbox bền vững"]
    OUTBOX -->|FHIR transaction| FHIR["FHIR server / simulator"]
    REPORT -. "sửa sau phát hành" .-> DRAFT["CorrectionDraft"]
    DRAFT --> REVIEW
    IN -. "version đổi khi đang xử lý" .-> RECON["Reconciliation"]
    RECON -. "operator giải quyết" .-> CASE
~~~

**Cách đọc:** nét liền là luồng phát hành bình thường; nét đứt là nhánh cần xử lý ngoại lệ. Correction cần Reviewer khác người tạo; thay đổi chỉ định sau khi bắt đầu xử lý không được âm thầm ghi đè snapshot đang làm.

~~~mermaid
flowchart LR
    STAFF["Nhân viên phòng xét nghiệm"] --> WEB["Angular web"]
    WEB -->|REST| API["ASP.NET Core API"]
    API --> APP["Application + Domain"]
    APP --> DB[("PostgreSQL")]
    WORKER[".NET Worker"] --> DB
    WORKER --> ADAPTER["FHIR adapter / Firely R4"]
    ADAPTER --> SIM["FHIR simulator local"]
    ADAPTER -. "M2: cấu hình đối tác" .-> PARTNER["FHIR server đối tác"]
~~~

API/worker dùng chung PostgreSQL; report, audit và outbox được ghi cùng transaction cục bộ. Worker gửi FHIR sau commit, nên EHR tạm thời không sẵn sàng không làm mất report đã phát hành nội bộ.

## Tài liệu cần đọc

Đọc theo thứ tự sau trước khi bắt đầu viết code:

1. [Yêu cầu sản phẩm và ca sử dụng](docs/01-product-requirements.md)
2. [Mô hình miền và quy tắc nghiệp vụ](docs/02-domain-workflow.md)
3. [Kiến trúc hệ thống và lựa chọn kỹ thuật](docs/03-system-architecture.md)
4. [Hợp đồng FHIR và tích hợp](docs/04-fhir-interoperability.md)
5. [API, giao diện và bảo mật](docs/05-api-ui-security.md)
6. [Độ tin cậy, vận hành và triển khai](docs/06-reliability-operations.md)
7. [Kiểm thử và tiêu chí nghiệm thu](docs/07-testing-acceptance.md)
8. [Kế hoạch triển khai](docs/08-implementation-plan.md)
9. [Prompt giao việc cho Gemini](docs/09-gemini-implementation-prompt.md)

## Mức độ khả thi và độ phức tạp

| Phạm vi | Khả thi | Độ phức tạp | Tin cậy kỹ thuật dự kiến sau nghiệm thu | Nhận xét |
|---|---:|---:|---:|---|
| Demo chạy độc lập với dữ liệu tổng hợp và FHIR simulator | Cao, 4/5 | Trung bình-cao, 4/5 | Cao, 4/5 nếu đạt toàn bộ AC và kiểm thử phục hồi | Có thể kiểm soát đầy đủ môi trường, schema và dữ liệu. |
| Kết nối một FHIR server thử nghiệm | Khá cao, 3/5 | Cao, 4/5 | Khá, 3/5 sau contract test với endpoint đích | Cần cấu hình đúng FHIR release, profile, capability, auth và terminology. |
| Thí điểm với một bệnh viện/phòng xét nghiệm | Chưa thể chấm chắc chắn | Rất cao, 5/5 | Chưa chấm | Phụ thuộc Implementation Guide, quyền truy cập, mã nội bộ, SOP và môi trường đối tác. |
| Vận hành lâm sàng thực tế | Chưa được đánh giá | Rất cao, 5/5 | Chưa đánh giá | Cần thẩm định nghiệp vụ, an toàn, bảo mật, quy định địa phương và quy trình chất lượng. |

Điểm tin cậy là mục tiêu kỹ thuật sau khi phần mềm được triển khai và nghiệm thu, không phải bằng chứng hiện có hoặc điểm về độ chính xác/chất lượng lâm sàng. Trạng thái hiện tại vẫn là M0: bộ tài liệu đã có, source và test chưa chạy.

~~~mermaid
flowchart LR
    M0["M0<br/>Đặc tả"] --> M1["M1<br/>Demo local<br/>4/5 khả thi"]
    M1 --> M2["M2<br/>FHIR test server<br/>3/5 khả thi"]
    M2 --> M3["M3<br/>Pilot đối tác<br/>IG + SOP + security review"]
    M3 --> M4["M4<br/>Production lâm sàng<br/>Chưa được đánh giá"]
~~~

Điểm 4/5 và 3/5 là ước lượng có điều kiện sau khi đạt tiêu chí nghiệm thu; hiện trạng vẫn là M0.

Ước lượng sơ bộ cho một developer làm demo có API, UI tối thiểu, PostgreSQL, FHIR simulator, outbox và kiểm thử là khoảng 3–5 tuần. Kết nối một hệ thống thật có thể cần thêm vài tuần hoặc lâu hơn sau khi có môi trường thử nghiệm và tài liệu tích hợp. Đây là ước lượng kỹ thuật, không phải cam kết lịch.

## Mục tiêu kỹ thuật của bản triển khai

- .NET 10 / C# 13 (pin `LangVersion` 13.0), ASP.NET Core Controllers, EF Core 10 và PostgreSQL.
- Một modular monolith gồm API, worker nền, ứng dụng web và các project thư viện dùng chung.
- FHIR R4 thông qua Firely .NET SDK R4; demo kiểm tra cấu trúc resource, còn partner profile/terminology được pin và kiểm tra ở remote integration.
- Demo mặc định dùng dữ liệu tổng hợp và simulator cục bộ, không gọi server ngoài. Có adapter riêng để kết nối FHIR server thật bằng cấu hình.
- Outbox bền vững trong PostgreSQL; phát hành báo cáo không phụ thuộc việc EHR đang trực tuyến.
- Audit nghiệp vụ, phân quyền, chống cập nhật mất dữ liệu, telemetry và phục hồi sau restart.

Các phiên bản package NuGet và npm ngoại trừ target .NET/EF Core phải được kiểm tra và khóa ở phiên bản ổn định, tương thích tại thời điểm Gemini triển khai. Firely SDK yêu cầu package theo đúng FHIR release; tài liệu chọn package R4 vì đặc tả demo chọn FHIR R4, không khẳng định R4 là phiên bản phù hợp cho mọi đối tác.

## Đọc về FHIR và tiêu chuẩn liên quan

- [FHIR R4 ServiceRequest](https://hl7.org/fhir/R4/servicerequest.html), [Specimen](https://hl7.org/fhir/R4/specimen.html), [Observation](https://hl7.org/fhir/R4/observation.html), [DiagnosticReport](https://hl7.org/fhir/R4/diagnosticreport.html)
- [FHIR R4 HTTP và transaction](https://hl7.org/fhir/R4/http.html), [CapabilityStatement](https://hl7.org/fhir/R4/capabilitystatement.html)
- [Firely .NET SDK 6](https://docs.fire.ly/projects/Firely-NET-SDK/en/sdk6/), [gói R4](https://docs.fire.ly/projects/Firely-NET-SDK/en/sdk6/getting-started.html), [validation](https://docs.fire.ly/projects/Firely-NET-SDK/en/latest/validation.html)
- [SMART App Launch](https://hl7.org/fhir/smart-app-launch/) và [Backend Services](https://hl7.org/fhir/smart-app-launch/backend-services.html)
- [LOINC User Guide](https://loinc.org/kb/users-guide/introduction/) và [UCUM](https://ucum.org/ucum)
- [ISO 15189:2022](https://www.iso.org/standard/76677.html) mô tả yêu cầu về chất lượng và năng lực phòng xét nghiệm. Tài liệu tham khảo ISO để định hướng câu hỏi nghiệp vụ; project này không chứng nhận hay khẳng định tuân thủ ISO 15189.

## Quy ước bàn giao

- Mọi luồng demo dùng dữ liệu giả, không dùng thông tin bệnh nhân thật.
- FHIR là hợp đồng trao đổi. Domain model nội bộ không được dùng trực tiếp làm FHIR resource.
- Firely SDK là thư viện mô hình/client/serialize/validate, không phải FHIR server.
- Không tuyên bố “exactly once” qua mạng. Thiết kế dựa trên outbox bền vững, gửi ít nhất một lần, idempotency và đối soát.
- Tài liệu này là đặc tả; trạng thái runnable chỉ được ghi nhận sau khi source được tạo và kiểm tra theo tiêu chí trong tài liệu nghiệm thu.

