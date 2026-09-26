import { Observable } from 'rxjs';
import { BoardDto, BoardDetailDto } from '../models/board.model';
import { ColumnDto } from '../models/column.model';
import { TaskDto } from '../models/task.model';
import { ActivityDto } from '../models/activity.model';
import { CreateBoardRequest, UpdateBoardRequest } from '../requests/board.request';
import { CreateColumnRequest, UpdateColumnRequest, ReorderColumnsRequest } from '../requests/column.request';
import { CreateTaskRequest, UpdateTaskRequest, MoveTaskRequest } from '../requests/task.request';

/**
 * Dependency Inversion Principle (Nguyên lý đảo ngược phụ thuộc):
 * Tầng Domain định nghĩa một 'hợp đồng' (contract) bằng abstract class này.
 * Nó chỉ nói CHO BIẾT cần làm gì (lấy boards, tạo task, v.v.) chứ không quan tâm LÀM THẾ NÀO.
 * 
 * Tầng Infrastructure (ngoài cùng) sẽ là nơi triển khai cụ thể hợp đồng này (thực hiện gọi HTTP đến API thật).
 * Lợi ích:
 * - Tách biệt logic nghiệp vụ khỏi các công nghệ cụ thể (Angular HttpClient).
 * - Dễ dàng thay thế triển khai (swap implement): Ví dụ khi viết Unit Test,
 *   ta có thể tự tạo một class MockBoardApi implement IBoardApiPort trả về dữ liệu giả 
 *   mà không cần phải đổi một dòng code nào trong tầng nghiệp vụ.
 */
export abstract class IBoardApiPort {
  // --- Board ---
  abstract getBoards(): Observable<BoardDto[]>;
  abstract getBoard(id: string): Observable<BoardDetailDto>;
  abstract createBoard(req: CreateBoardRequest): Observable<BoardDto>;
  abstract updateBoard(id: string, req: UpdateBoardRequest): Observable<void>;
  abstract deleteBoard(id: string): Observable<void>;

  // --- Column ---
  abstract getColumns(boardId: string): Observable<ColumnDto[]>;
  abstract createColumn(boardId: string, req: CreateColumnRequest): Observable<ColumnDto>;
  abstract updateColumn(id: string, req: UpdateColumnRequest): Observable<void>;
  abstract reorderColumns(req: ReorderColumnsRequest): Observable<void>;
  abstract deleteColumn(id: string): Observable<void>;

  // --- Task ---
  abstract getTasks(columnId: string): Observable<TaskDto[]>;
  abstract createTask(columnId: string, req: CreateTaskRequest): Observable<TaskDto>;
  abstract updateTask(id: string, req: UpdateTaskRequest): Observable<void>;
  abstract moveTask(id: string, req: MoveTaskRequest): Observable<void>;
  abstract deleteTask(id: string): Observable<void>;

  // --- Activity ---
  abstract getActivities(boardId: string): Observable<ActivityDto[]>;
}
