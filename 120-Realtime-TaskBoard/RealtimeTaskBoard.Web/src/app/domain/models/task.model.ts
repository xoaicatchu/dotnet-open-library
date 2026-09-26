/**
 * Task là đơn vị công việc nhỏ nhất trong mô hình Kanban.
 * Mỗi Task đại diện cho một yêu cầu, một lỗi, hoặc một tính năng cần thực hiện.
 */

// Định nghĩa các mức độ ưu tiên của công việc bằng type union
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';

/**
 * Data Transfer Object (DTO) cho Task.
 * Hậu tố DTO chỉ ra rằng đây là đối tượng để truyền dữ liệu giữa các hệ thống (client-server).
 * Sử dụng interface thay vì class để tận dụng TypeScript type checking tại compile time
 * mà không sinh ra mã JavaScript khi chạy, giúp giảm kích thước bundle.
 */
export interface TaskDto {
  id: string;
  columnId: string;
  title: string;
  description?: string;
  priority: TaskPriority;
  assignee?: string;
  position: number;
  labels: string[];
  dueDate?: Date | string;
  createdAt: Date | string;
  updatedAt: Date | string;
}
