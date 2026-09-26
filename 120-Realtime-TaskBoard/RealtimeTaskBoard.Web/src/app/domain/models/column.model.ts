import { TaskDto } from './task.model';

/**
 * Column (Cột) đại diện cho một trạng thái của công việc trong quy trình Kanban.
 * Ví dụ: Todo (Cần làm), In Progress (Đang làm), Done (Hoàn thành).
 */
export interface ColumnDto {
  id: string;
  boardId: string;
  name: string;
  position: number;
  color?: string;
}

/**
 * Mở rộng từ ColumnDto, chứa thêm danh sách các Task nằm trong cột này.
 * Cấu trúc phân cấp giúp dễ dàng hiển thị dữ liệu trên giao diện.
 */
export interface ColumnWithTasksDto extends ColumnDto {
  tasks: TaskDto[];
}
