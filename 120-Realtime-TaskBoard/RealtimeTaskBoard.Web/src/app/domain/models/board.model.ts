import { ColumnWithTasksDto } from './column.model';

/**
 * Board (Bảng) trong Kanban là nơi quản lý toàn bộ luồng công việc của một dự án hoặc nhóm.
 * Sử dụng interface vì Angular/TypeScript khuyến khích dùng interface cho các DTO (Data Transfer Object)
 * từ API trả về, giúp TypeScript kiểm tra kiểu dữ liệu (type check) ở compile-time
 * nhưng không tạo ra thêm code ở runtime (như class).
 *
 * Hậu tố DTO biểu thị đây là dữ liệu thuần túy (anemic model) truyền nhận qua HTTP/API,
 * phân biệt với các class chứa cả dữ liệu lẫn logic (rich model).
 */
export interface BoardDto {
  id: string;
  name: string;
  description?: string;
  createdAt: Date | string;
  updatedAt: Date | string;
}

/**
 * Phiên bản chi tiết của Board, chứa thêm danh sách các cột (và các task bên trong).
 */
export interface BoardDetailDto extends BoardDto {
  columns: ColumnWithTasksDto[];
}
