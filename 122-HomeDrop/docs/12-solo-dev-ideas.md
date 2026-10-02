# 12 — Mười ý tưởng mới cho solo dev và cách thử thu tiền

Ngày nghiên cứu: 2026-09-28. Tập trung người dùng cá nhân, chủ yếu mobile, thao tác với tệp/camera/âm thanh/cảm biến. Không phụ thuộc việc xây marketplace hoặc ký hợp đồng với hàng loạt doanh nghiệp.

**Phạm vi ước lượng:** solo dev có kinh nghiệm mobile, 30 giờ/tuần, một hệ điều hành, khoảng 4–8 tuần cho MVP nhỏ. Cần thêm thời gian sửa lỗi thiết bị, xét duyệt store và hỗ trợ. Mọi giá VND là **giá thử đề xuất**, không phải doanh thu chắc chắn hoặc giá của đối thủ.

## 1. Đọc mẫu đan móc khi đang làm

Người đan móc mở mẫu PDF, đánh dấu dòng đang làm, đếm hàng và số lần lặp. Đóng app rồi mở lại vẫn đúng vị trí. Nút lớn dùng bằng một tay khi tay kia giữ sản phẩm.

| Mục | Đề xuất |
|---|---|
| MVP 4–6 tuần | Nhập PDF, zoom, vạch dòng, bộ đếm hai tầng, undo, nhiều sản phẩm đang làm, xuất backup |
| Phạm vi nhỏ | Người dùng tự cấu hình; không hiểu mẫu bằng AI hoặc bán mẫu có bản quyền |
| Thu tiền | Miễn phí một sản phẩm; 149.000đ mua một lần để mở nhiều sản phẩm |
| Khách đầu tiên | Người hướng dẫn đan móc và học viên, demo bằng mẫu họ đang dùng |
| Rủi ro | Tốn công thiết lập; app hiện có đủ tốt |
| Gate thử bán | 20 người; ≥8 quay lại ba buổi và ≥5 mua ở mức giá thử |

Tham khảo nghiệp vụ: [knitCompanion](https://www.knitcompanion.com/) có đánh dấu mẫu, bộ đếm và tính năng trả phí. Chưa có bằng chứng về khoảng trống riêng tại Việt Nam.

## 2. Xem lại động tác cho người tự tập thể thao

Người chơi cầu lông hoặc tập nhảy quay hai clip, chọn một mốc tương ứng, xem cạnh nhau, tua chậm và vẽ đường tham chiếu. Chọn một môn trước để có workflow rõ.

| Mục | Đề xuất |
|---|---|
| MVP 6–8 tuần | Hai clip ≤60 giây, sync mốc thủ công, slow motion, bước khung hình theo decoder, vẽ đường, xuất ảnh so sánh |
| Phạm vi nhỏ | Chưa AI chấm kỹ thuật, xuất video ghép hoặc mạng xã hội |
| Thu tiền | Ba phiên miễn phí; 199.000đ mua một lần |
| Khách đầu tiên | Một câu lạc bộ phong trào có người thường tự quay động tác |
| Rủi ro | Góc quay lệch làm so sánh kém; video decoder đa thiết bị |
| Gate thử bán | 15 người dùng clip của mình; ≥6 dùng hai buổi, ≥4 mua |

Tham khảo: [Onform](https://onform.com/pricing/) có phân tích video và gói cho cá nhân. MVP tập trung người tự xem lại, chưa đưa ra chẩn đoán hoặc lời khuyên sức khỏe.

## 3. Xóa thông tin riêng tư khỏi ảnh và PDF trước khi gửi

Người dùng khoanh vùng địa chỉ, số tài khoản hoặc mã cá nhân rồi xuất bản chia sẻ. Thiết kế xử lý tại máy, không bắt đưa giấy tờ lên server.

| Mục | Đề xuất |
|---|---|
| MVP 6–8 tuần | Ảnh/PDF ≤20 trang, khoanh vùng, preview, dựng PDF mới từ ảnh đã xử lý, bỏ metadata, chia sẻ |
| Phạm vi nhỏ | Không OCR; bản xuất không còn text layer tìm kiếm; không sửa bản gốc |
| Thu tiền | Ba lần xuất miễn phí; 149.000đ mua một lần |
| Khách đầu tiên | Người thường tự nộp hồ sơ cá nhân từ điện thoại; demo tài liệu giả |
| Rủi ro | Nếu chỉ phủ hình đen lên text cũ vẫn có thể lộ dữ liệu; phải kiểm tra text/attachment/metadata của bản xuất |
| Gate thử bán | 20 người có nhu cầu thật trong tháng; ≥5 mua, không thu giấy tờ của họ |

Tham khảo: [PDF Expert trên iOS](https://pdfexpert.com/ios/how-to-edit-pdf) có chức năng redaction. Chọn thư viện PDF phải kiểm tra license thương mại và khả năng render an toàn; đây là hướng cần độ cẩn thận bảo mật cao hơn các tiện ích khác.

## 4. Chọn ảnh giữ lại trong một loạt ảnh chụp liên tiếp

Phụ huynh chụp hàng chục ảnh con trong cùng khoảnh khắc. App gom ảnh gần thời điểm, cho phóng to so sánh hai tấm và chọn giữ. Hành động xóa luôn qua xác nhận rõ.

| Mục | Đề xuất |
|---|---|
| MVP 5–7 tuần | Quyền ảnh giới hạn, nhóm theo giờ, so sánh đôi, chọn giữ, hàng đợi xóa, xác nhận OS |
| Phạm vi nhỏ | Không nhận dạng khuôn mặt, AI chấm đẹp hoặc tự xóa |
| Thu tiền | Miễn phí vài nhóm; 99.000đ mua một lần |
| Khách đầu tiên | Cộng đồng phụ huynh chụp nhiều ảnh |
| Rủi ro | Xóa nhầm; cloud asset chưa tải; khác biệt quyền thư viện |
| Gate thử bán | 20 người xử lý ≥100 ảnh/người; ≥5 mua; không xóa ngoài lựa chọn |

Tham khảo: [Slidebox](https://slidebox.co/) có cách tổ chức ảnh bằng cử chỉ. Giả thuyết khác biệt là thao tác so sánh từng loạt có đủ hữu ích để người dùng chọn mua.

## 5. Teleprompter cho người tự quay video

Người dùng đọc chữ đặt gần camera trong khi ghi hình, giảm việc nhớ hết bài nói. Tập trung quay một video hoàn chỉnh trên điện thoại với ít thao tác.

| Mục | Đề xuất |
|---|---|
| MVP 6–8 tuần | Nhập chữ, font/tốc độ cuộn, đếm ngược, camera trước1080p, lưu video, đánh dấu đoạn muốn quay lại |
| Phạm vi nhỏ | Không AI viết kịch bản, voice-follow, caption hay editor video đầy đủ |
| Thu tiền | Hai kịch bản miễn phí; 199.000đ mua một lần |
| Khách đầu tiên | Người đang tập quay chia sẻ cá nhân/thuyết trình |
| Rủi ro | Miễn phí đã đủ; vị trí chữ gây ánh mắt lệch; camera/micro đa thiết bị |
| Gate thử bán | 20 người quay xong; ≥8 quay lần hai và ≥5 mua |

Tham khảo: [Teleprompter.com](https://www.teleprompter.com/pricing) có free/paid plans. Lợi thế cần chứng minh bằng trải nghiệm và nhu cầu, không chỉ thêm bản tiếng Việt.

## 6. Máy nghe audiobook từ tệp cá nhân

Người có sách nói/nội dung âm thanh hợp pháp nhập tệp vào máy, nghe tiếp đúng vị trí, bookmark và điều khiển khi tắt màn hình.

| Mục | Đề xuất |
|---|---|
| MVP 5–7 tuần | MP3/M4B, nhóm sách/chương, resume, tốc độ phát, bookmark, sleep timer, tai nghe/lockscreen |
| Phạm vi nhỏ | Không bán nội dung, tải từ dịch vụ khác, gỡ DRM, cloud hoặc smartwatch |
| Thu tiền | Hai sách miễn phí; 99.000đ mua một lần |
| Khách đầu tiên | Người đã sở hữu audiobook dạng tệp hoặc ghi âm dài |
| Rủi ro | Cạnh tranh app miễn phí; tệp lỗi và quyền truy cập thay đổi |
| Gate thử bán | 20 người nhập tệp thật; ≥10 nghe ba ngày và ≥5 mua |

Tham khảo: [BookPlayer](https://apps.apple.com/us/app/bookplayer/id1138219998) hỗ trợ nhập tệp, chương, tốc độ và hẹn dừng. Chỉ đủ các tính năng cơ bản chưa tạo khác biệt; ưu tiên người gặp khó ở nhập/quản lý bộ tệp.

## 7. Điều khiển nhịp tập nhảy dây và chạy–nghỉ

Người dùng tự đặt chuỗi tập/nghỉ; âm thanh báo từng giai đoạn để họ không cần nhìn màn hình. Ứng dụng phải xử lý âm thanh nền, cuộc gọi và tiếp tục đúng nhịp.

| Mục | Đề xuất |
|---|---|
| MVP 4–5 tuần | Nhiều phase/lặp, giọng nhắc tiếng Việt, pause/resume, lockscreen audio, lưu mẫu và share mẫu |
| Phạm vi nhỏ | Không viết giáo án y tế, đếm nhảy bằng cảm biến hay app đồng hồ |
| Thu tiền | Ba mẫu tự tạo miễn phí; 79.000đ mua một lần |
| Khách đầu tiên | Nhóm nhảy dây/chạy phong trào |
| Rủi ro | Timer miễn phí đã đủ; audio interruption và vendor background |
| Gate thử bán | 25 người trong14 ngày; ≥10 tập bốn buổi và ≥5 mua |

Tham khảo: [Seconds](https://www.intervaltimer.com/app) có interval timer, voice cue và mở khóa trả phí. Cần chứng minh workflow một môn cụ thể đáng tiền.

## 8. Thẻ NFC cho các góc sinh hoạt trong nhà

Chạm vào thẻ cạnh góc đọc sách để mở âm thanh; thẻ ở thảm tập mở bộ đếm bài tập; thẻ cạnh máy giặt mở timer đã đặt. Người dùng không phải tìm trong danh sách app.

| Mục | Đề xuất |
|---|---|
| MVP 4–6 tuần, Android | Ghi NDEF, ba hành động trong app, mẫu, xác nhận ghi, đọc kiểm tra, sửa mapping |
| Phạm vi nhỏ | Chỉ thẻ người dùng sở hữu; không giả lập thẻ ra vào/thanh toán hoặc thay mọi system setting |
| Thu tiền | Hai mẫu miễn phí; 99.000đ mua một lần; thẻ mua riêng |
| Khách đầu tiên | Người đã có NFC phone và thẻ trắng |
| Rủi ro | Cần phụ kiện; hành vi NFC khác nhau theo model và trạng thái khóa |
| Gate thử bán | 15 người thiết lập hai thẻ; ≥6 dùng sau một tuần, ≥4 mua |

Tham khảo: [NFC Tools Pro](https://www.wakdev.com/en/apps.html). Phạm vi nên chọn vài tình huống cực rõ, không làm lại cả hệ thống automation.

## 9. Số hóa âm bản ảnh gia đình

Người có phim âm bản cũ dùng nguồn sáng nền và camera để chụp, đảo màu, cân bằng màu thủ công và lưu ảnh số.

| Mục | Đề xuất |
|---|---|
| MVP 6–8 tuần | Hướng dẫn bố trí, crop một frame, khóa exposure nếu hỗ trợ, invert/cân màu, preset một cuộn, JPEG export |
| Phạm vi nhỏ | Không AI phục hồi, RAW, tự quét nhiều khung, hoặc cam kết chất lượng scan chuyên dụng |
| Thu tiền | Preview miễn phí; 149.000đ mua một lần để xuất không giới hạn |
| Khách đầu tiên | Nhóm chụp film và gia đình còn âm bản |
| Rủi ro | Chất lượng phụ thuộc setup/camera/phim; nhu cầu có thể chỉ một đợt |
| Gate thử bán | 15 người dùng phim của họ; ≥5 mua sau khi xem ảnh mẫu đầy đủ |

Tham khảo: [FilmBox](https://photomyne.com/filmbox). Đây là hướng xử lý ảnh/camera thực tế; cần bộ phim mẫu nhiều điều kiện để tránh tối ưu một demo duy nhất.

## 10. Xem đường nắng tại ban công

Người trồng cây hoặc chọn vị trí ngồi đưa điện thoại lên để xem hướng mặt trời theo ngày/giờ. App kết hợp tính vị trí mặt trời với cảm biến hướng và camera.

| Mục | Đề xuất |
|---|---|
| MVP 6–8 tuần | Vị trí, tính góc mặt trời, ngày/giờ, compass diagram, camera overlay, lưu ảnh ghi chú, hướng dẫn hiệu chỉnh |
| Phạm vi nhỏ | Không biết vật che nắng, không dự báo mây hoặc tư vấn cây; không cam kết đo số giờ nắng thực tế |
| Thu tiền | Ngày hiện tại miễn phí; 99.000đ mua một lần để xem cả năm |
| Khách đầu tiên | Người trồng cây ban công, demo tại nhà thực |
| Rủi ro | La bàn sai, kỳ vọng AR chính xác, tần suất thấp |
| Gate thử bán | 20 người kiểm tra tại nhà; ≥5 trả tiền sau khi hiểu giới hạn |

Tham khảo: [Sun Surveyor](https://www.sunsurveyor.com/). Tính vị trí mặt trời đã có sản phẩm; cần kiểm chứng ngách chăm ban công có nhu cầu chi tiền riêng.

## Kinh tế của một app nhỏ

Mua một lần phù hợp để thử tiện ích chủ yếu xử lý trên máy; thuê bao chỉ đáng cân nhắc khi có giá trị và chi phí lặp lại. Kịch bản minh họa:

| Thành phần | Giả định |
|---|---:|
| Giá | 149.000đ |
| Phí cửa hàng | 15% |
| Hoàn tiền | 3% |
| Chi phí có một người mua | 30.000đ |
| Dự phòng hỗ trợ/người mua | 10.000đ |
| Còn lại trước chi phí cố định, thuế, công phát triển | ~82.851đ/giao dịch |

`149.000 × 0,85 × 0,97 − 30.000 − 10.000 ≈ 82.851đ`. 100 giao dịch =14,9 triệu doanh thu gộp, còn khoảng8,29 triệu theo giả định. Đây không là dự báo doanh số; tháng sau vẫn cần người mua mới. Bao gồm phí developer account, thiết bị, library licensing, signing/support khi tính hòa vốn thực tế.

Mức phí15% phụ thuộc chương trình/loại giao dịch/thị trường: [Apple Small Business Program](https://developer.apple.com/app-store/small-business-program/), [Google Play service fees](https://support.google.com/googleplay/android-developer/answer/112622?hl=en). Giá trong bảng từng ý tưởng là đề xuất thử nghiệm riêng, không được trích từ trang đối thủ.

## Ba hướng nên xác thực trước

| Ưu tiên | Hướng | Lý do | Cần chứng minh |
|---|---|---|---|
| 1 | Mẫu đan móc | Workflow hẹp, cộng đồng rõ, quan sát được dùng lặp lại | Thiết lập nhanh và người dùng thực sự mua |
| 2 | Xem lại động tác | Demo thấy ngay giá trị, không backend | Video riêng của người dùng đủ hữu ích |
| 3 | Teleprompter | Tác vụ cụ thể, dễ đưa cho người mới thử | Họ chọn mua giữa nhiều app hiện có |

Đây là thứ tự xét theo khả năng thực hiện và tổ chức thử nhỏ, không theo số liệu quy mô thị trường.

~~~mermaid
flowchart TD
    A["Chọn một ngách + một OS"] --> B["Trao đổi 10 người có nhu cầu thật"]
    B --> C["Demo trên dữ liệu của họ"]
    C --> D["MVP nhỏ + giá công khai"]
    D --> E["15–25 người dùng, 14–21 ngày"]
    E --> F{"Có dùng lại + trả tiền?"}
    F -->|Có| G["Sửa lỗi, mở kênh bán"]
    F -->|Chưa| H["Thử một thay đổi về giá trị / UX / giá"]
    H --> I{"Tín hiệu tốt hơn?"}
    I -->|Có| G
    I -->|Không| J["Dừng hoặc đổi ngách"]
~~~

Các bước tiếp theo: chọn một ý tưởng, viết lời hứa một câu, hỏi về lần gần nhất người dùng gặp việc đó, dựng demo, công khai giá dự kiến, rồi đo hành vi thật. Không đánh đồng thích thử với đã trả tiền. Chỉ thêm hệ điều hành thứ hai/cloud sau khi có dữ liệu về dùng lặp lại, thanh toán và năng lực hỗ trợ.
