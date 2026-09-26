/**
 * Các loại hành động có thể xảy ra trên hệ thống.
 */
export type ActionType = 'Created' | 'Updated' | 'Deleted' | 'Moved';

/**
 * Activity log (Nhật ký hoạt động) ghi lại lịch sử các thao tác trên board.
 * Hữu ích cho việc theo dõi tiến độ, kiểm toán (audit), hoặc hiển thị timeline cho người dùng.
 */
export interface ActivityDto {
  id: string;
  boardId: string;
  actorName: string;
  actionType: ActionType;
  entityType: string; // Ví dụ: 'Task', 'Column', 'Board'
  entityId: string;
  oldValue?: string;
  newValue?: string;
  timestamp: Date | string;
}
