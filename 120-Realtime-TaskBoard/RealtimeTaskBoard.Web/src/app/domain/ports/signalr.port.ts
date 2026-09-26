import { Observable } from 'rxjs';

/**
 * ISignalRPort định nghĩa hợp đồng để giao tiếp realtime.
 * 
 * Giải thích SignalR:
 * SignalR là thư viện real-time của Microsoft dành cho ASP.NET Core. Nó sử dụng WebSocket 
 * (và fallback xuống Server-Sent Events hoặc Long Polling nếu cần) để thiết lập kết nối hai chiều
 * giữa Client (Angular) và Server (.NET).
 * 
 * Điểm mạnh:
 * - Server có thể CHỦ ĐỘNG đẩy dữ liệu (push) xuống cho toàn bộ client ngay khi có thay đổi.
 * - Tránh được việc client phải liên tục gọi API hỏi server xem có dữ liệu mới không (polling),
 *   giúp tiết kiệm băng thông và phản hồi tức thời.
 */
export abstract class ISignalRPort {
  /**
   * Khởi tạo kết nối tới SignalR Hub và tham gia vào nhóm của board cụ thể
   */
  abstract connect(boardId: string): Promise<void>;

  /**
   * Ngắt kết nối khỏi SignalR Hub
   */
  abstract disconnect(boardId: string): Promise<void>;

  /**
   * Stream lắng nghe các sự kiện liên quan đến Board (ví dụ: đổi tên board, thêm cột)
   */
  abstract boardEvents$: Observable<{ type: string; data: any }>;

  /**
   * Stream lắng nghe các sự kiện liên quan đến Task (ví dụ: tạo, sửa, xóa, di chuyển task)
   */
  abstract taskEvents$: Observable<{ type: string; data: any }>;

  /**
   * Stream lắng nghe các hoạt động để cập nhật log
   */
  abstract activityEvents$: Observable<{ type: string; data: any }>;
}
