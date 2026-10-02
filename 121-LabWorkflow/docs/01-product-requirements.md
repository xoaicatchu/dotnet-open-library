# 01. Yêu cầu sản phẩm và ca sử dụng

## 1. Tầm nhìn

Xây dựng một ứng dụng điều phối công việc xét nghiệm có thể chạy độc lập trong repo học tập, thể hiện một luồng nghiệp vụ đầu-cuối:

**EHR gửi chỉ định → phòng xét nghiệm nhận việc → lấy/nhận mẫu → nhập kết quả → người có quyền duyệt → ứng dụng phát hành báo cáo → worker gửi báo cáo FHIR đến EHR.**

Ứng dụng có hai chế độ:

1. **Demo:** tự chạy với dữ liệu tổng hợp và FHIR simulator; không cần tài khoản hay máy chủ y tế bên ngoài.
2. **Remote FHIR:** kết nối một FHIR server R4 được cấu hình rõ; phải kiểm tra capability, profile, terminology và cơ chế xác thực của máy chủ đích.

## 2. Người dùng và quyền

| Vai trò | Công việc |
|---|---|
| Collector | Xem việc chờ lấy mẫu; tạo/đọc mã barcode; ghi thời điểm và loại mẫu; ghi nhận mẫu bị từ chối nếu được phân quyền. |
| LabTechnician | Tiếp nhận/nhận mẫu; nhập kết quả nháp; hoàn tất bộ kết quả để gửi duyệt. |
| Reviewer | Rà soát tính đầy đủ của kết quả và phát hành báo cáo. |
| IntegrationOperator | Theo dõi kết nối FHIR và outbox; xem lỗi đã loại bỏ thông tin nhạy cảm; yêu cầu retry hoặc đánh dấu cần đối soát. |
| Administrator | Quản lý danh mục demo, vai trò, cấu hình và health state. Không có quyền sửa âm thầm báo cáo đã phát hành. |

Một người dùng có thể có nhiều quyền. Bản demo phải yêu cầu quyền Reviewer khi phát hành. Chính sách người nhập kết quả có được tự duyệt hay không phải là cấu hình; mặc định demo yêu cầu người duyệt khác người nhập cuối cùng để thể hiện kiểm soát hai người, nhưng không tuyên bố đây là quy tắc áp dụng cho mọi phòng xét nghiệm.

## 3. Phạm vi chức năng

### Bao gồm

- Nhận chỉ định xét nghiệm FHIR R4 hoặc nạp fixture FHIR trong demo.
- Tạo worklist nội bộ theo thời gian, trạng thái, loại xét nghiệm và mã tiếp nhận.
- Theo dõi nhiều specimen và nhiều Observation/result component cho một chỉ định; trong M1, mỗi ServiceRequest ánh xạ đúng một LabCase và mỗi LabCase chỉ phát hành một DiagnosticReport ban đầu cùng các correction revision.
- Tạo accession identifier duy nhất trong phạm vi phòng xét nghiệm.
- Ghi nhận lấy, nhận, chấp nhận hoặc từ chối mẫu kèm lý do.
- Nhập kết quả kiểu số, mã hóa hoặc văn bản theo loại kết quả đã khai báo.
- Rà soát, phát hành phiên bản báo cáo không thể ghi đè.
- Tạo Observation và DiagnosticReport; liên kết ngược ServiceRequest và Specimen. Không hỗ trợ phát hành từng phần hoặc nhiều DiagnosticReport cho một LabCase trong M1.
- Lưu sự kiện outbox trong cùng giao dịch DB với việc phát hành.
- Worker gửi transaction FHIR đến simulator hoặc remote FHIR server.
- Hiển thị audit timeline và trạng thái gửi riêng với trạng thái báo cáo.
- Hỗ trợ API .http, Swagger/OpenAPI, log có cấu trúc và health checks.

### Không bao gồm trong bản đầu

- EHR, Patient Master Index hoặc hồ sơ sức khỏe đầy đủ.
- Quyết định chẩn đoán, diễn giải kết quả, cảnh báo lâm sàng hoặc tự động chọn ngưỡng bình thường.
- Giao tiếp trực tiếp với analyzer; HL7 v2/ASTM/device drivers.
- Quản lý thuốc, thanh toán/bảo hiểm, lịch hẹn, kho hóa chất, nhân sự hoặc kiểm chuẩn thiết bị.
- Multi-tenant SaaS; phân vùng nhiều cơ sở; lưu trữ hồ sơ quốc gia.
- Ký số pháp lý, workflow chứng nhận chất lượng hoàn chỉnh hoặc tuyên bố đạt chuẩn ISO.
- Chuyển báo cáo PDF làm hợp đồng dữ liệu chính. PDF có thể là phần mở rộng; FHIR vẫn là cấu trúc trao đổi.
- Nhập dữ liệu hoặc kiểm thử với PHI thật.

## 4. Ca sử dụng

~~~mermaid
flowchart LR
    EHR["EHR / FHIR server"] --> UC01["UC-01 Nhận chỉ định"]
    UC01 --> UC02["UC-02 Tìm và nhận việc"]
    UC02 --> UC03["UC-03 Lấy / đăng ký specimen"]
    UC03 --> UC04["UC-04 Tiếp nhận / từ chối specimen"]
    UC04 --> UC05["UC-05 Nhập kết quả"]
    UC05 --> UC06["UC-06 Gửi duyệt / Reviewer phát hành"]
    UC06 --> UC07["UC-07 Gửi báo cáo FHIR"]
    UC07 --> UC08["UC-08 Correction sau phát hành"]
    EHR -. "cập nhật / hủy chỉ định" .-> UC09["UC-09 Hủy / đối soát case"]
    Operator["IntegrationOperator"] --> UC10["UC-10 Phục hồi tích hợp lỗi"]
    UC10 -. "retry có kiểm soát" .-> UC07
    Collector["Collector"] --> UC02
    Collector --> UC03
    Collector --> UC04
    Technician["LabTechnician"] --> UC02
    Technician --> UC04
    Technician --> UC05
    Technician --> UC06
    Technician --> UC08
    Reviewer["Reviewer"] --> UC06
    Reviewer --> UC08
~~~

*Cách đọc:* LabTechnician có thể gửi kết quả để duyệt và tạo correction draft; chỉ Reviewer phát hành, và correction phải được duyệt bởi người khác với người tạo draft.

*Cách đọc:* đường liền là luồng xử lý chính từ chỉ định đến giao báo cáo; đường chấm là nhánh phát sinh hoặc phục hồi. Các UC-01…UC-10 bên dưới quy định điều kiện, lỗi và quyền chi tiết; sơ đồ không thay thế các quy tắc đó.

### UC-01 — Nhận chỉ định

**Tác nhân:** hệ thống EHR/FHIR hoặc worker import.  
**Điều kiện:** server R4 được cấu hình hoặc đang chạy ở demo mode.  
**Luồng chính:**

1. Worker truy vấn ServiceRequest mới/đã cập nhật theo capability được server công bố.
2. Adapter kiểm tra resource type, status, intent, subject, mã xét nghiệm và profile.
3. Ứng dụng tìm danh tính order theo source + FHIR base URL + resource type + resource id; lưu từng version quan sát vào lịch sử dưới cùng IncomingOrder.
4. Tạo đúng một LabCase nếu đây là ServiceRequest mới; mỗi version sau đó vẫn trỏ về IncomingOrder/LabCase đó.
5. Nếu đã thấy cùng source + resource id + version key thì trả kết quả idempotent, không tạo bản trùng.
6. Nếu version mới đến trước khi lab nhận việc/claim và chưa có specimen/result, cập nhật accepted order snapshot trên cùng LabCase. Nếu work đã bắt đầu, giữ accepted version, lưu version mới và đánh dấu ReconciliationRequired; không sửa snapshot đang xử lý hoặc tạo case thứ hai.

**Kết quả:** chỉ định hợp lệ xuất hiện trong worklist với source-version history. Lỗi cấu trúc/mã/profile được lưu thành integration issue có thể xử lý; không biến chỉ định lỗi thành một ca có thể phát hành.

### UC-02 — Tìm và nhận việc

**Tác nhân:** Collector hoặc LabTechnician.  
**Luồng chính:** lọc worklist → mở case → kiểm tra mã chỉ định, loại mẫu và mã bệnh nhân tham chiếu → claim/nhận việc theo chính sách. Mọi thao tác ghi nhận actor, thời điểm và version dữ liệu.

**Tiêu chí:** danh sách hiển thị trạng thái vận hành nội bộ. M1 cho phép nhiều specimen trong một case, nhưng một ServiceRequest ánh xạ một LabCase với một chuỗi DiagnosticReport. Nếu chỉ định cần nhiều report/panel độc lập hoặc không khớp đúng một panel demo đã cấu hình, đưa vào reconciliation; M1 không tự tách case hoặc phát hành một phần.

### UC-03 — Lấy/đăng ký specimen

**Tác nhân:** Collector.  
**Luồng chính:** chọn loại mẫu đã cấu hình → sinh/scan accession identifier → ghi thông tin lấy mẫu và thời gian → tạo Specimen → in/hiển thị nhãn barcode ở chế độ demo.

**Ngoại lệ:** accession trùng, mẫu không tương thích, chỉ định đã hủy hoặc case có thay đổi nguồn chưa được giải quyết. API trả lỗi có mã ổn định; không tạo specimen một phần.

### UC-04 — Tiếp nhận hoặc từ chối specimen

**Tác nhân:** LabTechnician.  
**Luồng chính:** quét accession → đối chiếu case và loại mẫu → ghi thời điểm nhận → chấp nhận hoặc từ chối kèm mã/lý do. Trạng thái và actor được lưu trong transaction DB cùng audit entry.

**Ngoại lệ:** không tìm thấy accession, mẫu đã từ chối/đã nhận trước đó, sửa đổi không dùng đúng version. Không cho nhập kết quả từ mẫu bị từ chối.

### UC-05 — Nhập kết quả

**Tác nhân:** LabTechnician.  
**Luồng chính:** chọn panel/cấu hình test → nhập thời điểm/thời khoảng thực hiện có căn cứ → nhập đủ thành phần theo định nghĩa panel → kiểm tra kiểu dữ liệu, code và UCUM unit → lưu bản nháp. `enteredAt` do server ghi nhận riêng, không thay cho thời điểm xét nghiệm.

**Tiêu chí:** mã quan sát và đơn vị được mã hóa riêng; không dùng tên tự do làm code. Không tự đánh dấu cao/thấp/nguy cấp bằng một ngưỡng giả. Kết quả số lưu chính xác theo decimal; không diễn giải qua floating-point double.

### UC-06 — Gửi duyệt và phát hành

**Tác nhân:** LabTechnician, sau đó Reviewer.  
**Luồng chính:** kỹ thuật viên gửi bộ kết quả cùng thời điểm thực hiện xét nghiệm có căn cứ → ứng dụng kiểm tra đủ component, specimen hợp lệ, nguồn chỉ định còn phù hợp và không có xung đột version → Reviewer kiểm tra và xác nhận → DB transaction tạo report revision bất biến, audit event và outbox message. Mỗi lần phát hành gom toàn bộ kết quả của case vào một DiagnosticReport; không hỗ trợ phát hành từng phần.

**Tiêu chí:** chưa được Reviewer xác nhận thì không tạo FHIR DiagnosticReport final. M1 bắt buộc có TestExecutionTime để điền `DiagnosticReport.effective[x]`; không thay bằng `issued` hoặc tự tạo thời gian. `issued` ghi thời điểm phát hành. Báo cáo final không thể sửa âm thầm; correction tạo revision mới và được ánh xạ status FHIR `corrected` theo hợp đồng phiên bản của adapter. `preliminary`, `amended`, `appended` và phát hành nhiều report nằm ngoài M1.

### UC-07 — Gửi báo cáo về FHIR server

**Tác nhân:** Outbox worker.  
**Luồng chính:** claim message chưa gửi → dựng FHIR resource bundle từ revision snapshot → kiểm tra cấu trúc/profile → gửi transaction nếu server hỗ trợ → đọc transaction-response → lưu resource ids, ETags/locations khi có và đánh dấu Delivered.

**Ngoại lệ:** timeout/5xx/429 theo retry policy; 401/403 ngừng gửi và cần sửa auth; lỗi profile/4xx không tự retry vô hạn mà vào trạng thái cần xử lý. Nếu response mất sau khi server đã commit, thực hiện idempotent retry/đối soát.

### UC-08 — Sửa báo cáo đã phát hành

**Tác nhân:** LabTechnician, sau đó Reviewer khác người tạo correction.  
**Luồng chính:** kỹ thuật viên tạo correction draft dựa trên ReportRevision mới nhất, nhập snapshot đầy đủ của toàn bộ result components và reason bắt buộc → hệ thống kiểm tra báo cáo gốc đã Delivered/được đối soát, draft không xung đột và khác actor với Reviewer → Reviewer xem cả bản cũ, bản sửa và lý do → phát hành correction tạo ReportRevision bất biến mới, audit và outbox trong một DB transaction → gửi cập nhật version-aware cho cùng chuỗi DiagnosticReport với status `corrected` (và status Observation phù hợp nếu nội dung Observation được sửa) theo mapping đã cấu hình.

**Tiêu chí:** không sửa trực tiếp snapshot đã phát hành; không xóa dấu vết lần phát hành trước. Correction draft không phải ReportRevision và không được gửi FHIR trước khi Reviewer khác người tạo phát hành. Chỉ một draft đang chờ duyệt cho mỗi case; draft dựa trên revision cũ hoặc báo cáo chưa Delivered/đối soát không thể release. M1 chỉ hỗ trợ correction, không mô phỏng `amended` hoặc `appended`. Cách cập nhật logical id, ETag và version phải được kiểm thử với simulator; remote mode phải xác nhận mapping theo Implementation Guide của đối tác.

### UC-09 — Hủy chỉ định hoặc case

**Tác nhân:** Integration worker hoặc người có quyền.  
**Luồng chính:** tiếp nhận trạng thái hủy từ nguồn hoặc yêu cầu nội bộ → kiểm tra case đã xử lý đến đâu → hủy công việc chưa bắt đầu hoặc chuyển sang cần đối soát nếu specimen/result đã tồn tại → giữ toàn bộ lịch sử.

**Tiêu chí:** không được xóa case đã có dữ liệu vận hành chỉ vì upstream ServiceRequest đổi status.

### UC-10 — Phục hồi tích hợp lỗi

**Tác nhân:** IntegrationOperator.  
**Luồng chính:** xem danh sách pending/retry/needs-attention → xem mã lỗi không lộ PHI → sửa cấu hình/nguyên nhân → retry có kiểm soát hoặc tạo reconciliation task. Mọi thao tác retry/resolution được audit.

## 5. Đánh giá mức độ thực thi

| Mức | Kết quả | Điều kiện |
|---|---|---|
| M0 — Đặc tả | Bộ tài liệu này | Chưa có source/runtime; không gọi là ứng dụng hoàn thành. |
| M1 — Showcase chạy local | API, UI nhỏ, PostgreSQL, luồng mẫu-kết quả-review-release, FHIR fixtures/simulator và outbox | Không dùng PHI; không phụ thuộc server bên ngoài. |
| M2 — FHIR integration demo | Firely REST client, FHIR server test, cấu hình profile/auth/capability và contract tests | Có server thử nghiệm tương thích và test credentials. |
| M3 — Pilot một đối tác | Mappings và quy trình cụ thể, observability, runbook, security review | Cần partner IG, terminology, scopes, environment và SOP được chủ hệ thống chấp thuận. |
| M4 — Production lâm sàng | Được tổ chức triển khai, kiểm thử, vận hành và quản trị theo quy định áp dụng | Không thể xác nhận bằng code hoặc tài liệu mẫu này. |

FHIR mô tả hợp đồng trao đổi. ISO 15189 mô tả yêu cầu chất lượng/năng lực phòng xét nghiệm. Chúng không thay thế nhau và bộ tài liệu không tuyên bố tuân thủ các yêu cầu chất lượng, luật dữ liệu hay quy định y tế của một quốc gia. Các quy trình lấy mẫu, kiểm soát chất lượng, lưu trữ, thời hạn và giá trị lâm sàng phải do đơn vị chuyên môn/đối tác xác nhận trước pilot.

