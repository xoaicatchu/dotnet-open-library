import { Injectable, signal } from '@angular/core';
import { BoardDetailDto } from '../../domain/models/board.model';
import { ActivityDto } from '../../domain/models/activity.model';
import { ColumnWithTasksDto, ColumnDto } from '../../domain/models/column.model';
import { TaskDto } from '../../domain/models/task.model';

/**
 * State Management Service quản lý trạng thái cục bộ của giao diện Board.
 * 
 * Signal là gì?
 * Signal là một primitive (cơ chế) phản ứng (reactive) mới được giới thiệu từ Angular 16.
 * Nó tương tự như BehaviorSubject trong RxJS vì lưu trữ giá trị hiện tại,
 * nhưng nhẹ hơn và được tích hợp sâu vào hệ thống Change Detection của Angular.
 * Khi một signal thay đổi giá trị, Angular tự động biết và chỉ update những phần UI sử dụng signal đó.
 */
@Injectable({
  providedIn: 'root'
})
export class BoardStateService {
  // signal lưu trữ toàn bộ dữ liệu chi tiết của board hiện tại
  currentBoard = signal<BoardDetailDto | null>(null);
  
  // signal lưu trữ danh sách các hoạt động (activity log)
  activities = signal<ActivityDto[]>([]);

  /**
   * Cập nhật toàn bộ dữ liệu board vào state
   */
  setBoard(board: BoardDetailDto) {
    this.currentBoard.set(board);
  }

  /**
   * Cập nhật thông tin cơ bản của board
   */
  updateBoardInfo(updates: Partial<BoardDetailDto>) {
    this.currentBoard.update(board => board ? { ...board, ...updates } : null);
  }

  /**
   * Thêm một cột mới vào board
   * Giải thích: Sử dụng toán tử spread (`...`) để copy data cũ ra mảng mới và append data mới.
   * Điều này đảm bảo tính bất biến (immutable), giúp Angular Signal nhận diện sự thay đổi tham chiếu
   * và tự động trigger update giao diện.
   */
  addColumn(column: ColumnDto) {
    this.currentBoard.update(board => {
      if (!board) return board;
      const newColumnWithTasks: ColumnWithTasksDto = { ...column, tasks: [] };
      return {
        ...board,
        columns: [...board.columns, newColumnWithTasks].sort((a, b) => a.position - b.position)
      };
    });
  }

  /**
   * Xóa một cột khỏi board
   */
  removeColumn(columnId: string) {
    this.currentBoard.update(board => {
      if (!board) return board;
      return {
        ...board,
        columns: board.columns.filter(c => c.id !== columnId)
      };
    });
  }

  /**
   * Sắp xếp lại thứ tự các cột
   */
  reorderColumns(columnIds: string[]) {
    this.currentBoard.update(board => {
      if (!board) return board;
      const columnsCopy = [...board.columns];
      columnsCopy.sort((a, b) => {
        return columnIds.indexOf(a.id) - columnIds.indexOf(b.id);
      });
      return { ...board, columns: columnsCopy };
    });
  }

  /**
   * Thêm một task mới vào đúng cột của nó
   */
  addTask(task: TaskDto) {
    this.currentBoard.update(board => {
      if (!board) return board;
      
      const newColumns = board.columns.map(col => {
        if (col.id === task.columnId) {
          return {
            ...col,
            tasks: [...col.tasks, task].sort((a, b) => a.position - b.position)
          };
        }
        return col;
      });

      return { ...board, columns: newColumns };
    });
  }

  /**
   * Cập nhật thông tin một task đang có
   */
  updateTask(updatedTask: TaskDto) {
    this.currentBoard.update(board => {
      if (!board) return board;
      
      const newColumns = board.columns.map(col => {
        if (col.id === updatedTask.columnId) {
          return {
            ...col,
            tasks: col.tasks.map(t => t.id === updatedTask.id ? updatedTask : t)
          };
        }
        return col;
      });

      return { ...board, columns: newColumns };
    });
  }

  /**
   * Xóa task khỏi cột
   */
  removeTask(taskId: string, columnId: string) {
    this.currentBoard.update(board => {
      if (!board) return board;
      
      const newColumns = board.columns.map(col => {
        if (col.id === columnId) {
          return {
            ...col,
            tasks: col.tasks.filter(t => t.id !== taskId)
          };
        }
        return col;
      });

      return { ...board, columns: newColumns };
    });
  }

  /**
   * Di chuyển task giữa các cột hoặc thay đổi vị trí trong cùng cột
   */
  moveTask(task: TaskDto, sourceColumnId: string, targetColumnId: string) {
    this.currentBoard.update(board => {
      if (!board) return board;
      
      // Copy mảng columns để đảm bảo immutability
      let newColumns = [...board.columns];
      
      // Xóa task ở cột cũ
      newColumns = newColumns.map(col => {
        if (col.id === sourceColumnId) {
          return { ...col, tasks: col.tasks.filter(t => t.id !== task.id) };
        }
        return col;
      });
      
      // Thêm task vào cột mới
      newColumns = newColumns.map(col => {
        if (col.id === targetColumnId) {
          return { 
            ...col, 
            tasks: [...col.tasks, { ...task, columnId: targetColumnId }].sort((a, b) => a.position - b.position) 
          };
        }
        return col;
      });

      return { ...board, columns: newColumns };
    });
  }

  /**
   * Thêm log hoạt động mới (thêm vào đầu mảng)
   */
  addActivity(activity: ActivityDto) {
    this.activities.update(acts => [activity, ...acts]);
  }
}
