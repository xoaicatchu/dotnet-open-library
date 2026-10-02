import { Component, input, output, signal, inject } from '@angular/core';
import { CdkDragDrop, CdkDropList, CdkDrag, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { BoardFacade } from '../../application/facades/board.facade';
import { ColumnWithTasksDto } from '../../domain/models/column.model';
import { TaskDto } from '../../domain/models/task.model';
import { TaskCardComponent } from '../task-card/task-card.component';
import { TaskDialogComponent } from '../task-dialog/task-dialog.component';

/**
 * ╔══════════════════════════════════════════════════════════════════════╗
 * ║  Column Component - Hiển thị 1 cột trên bảng Kanban               ║
 * ╠══════════════════════════════════════════════════════════════════════╣
 * ║  Mỗi cột đại diện cho một trạng thái công việc:                   ║
 * ║  Todo → In Progress → Review → Done                               ║
 * ║                                                                    ║
 * ║  Angular CDK Drag & Drop:                                         ║
 * ║  - CdkDropList: Biến div thành vùng nhận kéo thả (drop zone)     ║
 * ║  - cdkDropListData: Bind array dữ liệu để CDK quản lý thứ tự    ║
 * ║  - CdkDrag: Đánh dấu phần tử con có thể kéo được                ║
 * ║  - CdkDragDrop event chứa:                                        ║
 * ║    • previousContainer: danh sách gốc (cột cũ)                   ║
 * ║    • container: danh sách đích (cột mới)                         ║
 * ║    • previousIndex: vị trí cũ trong cột gốc                      ║
 * ║    • currentIndex: vị trí mới trong cột đích                     ║
 * ║  - moveItemInArray: Di chuyển item TRONG CÙNG danh sách          ║
 * ║  - transferArrayItem: Chuyển item GIỮA HAI danh sách             ║
 * ╚══════════════════════════════════════════════════════════════════════╝
 */
@Component({
  selector: 'app-column',
  standalone: true,
  imports: [CdkDropList, CdkDrag, TaskCardComponent, TaskDialogComponent],
  templateUrl: './column.component.html',
  styleUrl: './column.component.css'
})
export class ColumnComponent {
  /** Dữ liệu cột (bao gồm danh sách task bên trong) - nhận từ parent */
  column = input.required<ColumnWithTasksDto>();

  private boardFacade = inject(BoardFacade);
  showTaskDialog = signal(false);

  /**
   * Xử lý sự kiện kéo thả task.
   * Khi user thả task vào vị trí mới, CDK gọi hàm này với event chứa
   * thông tin cột gốc, cột đích, và vị trí trước/sau.
   */
  drop(event: CdkDragDrop<TaskDto[]>) {
    if (event.previousContainer === event.container && event.previousIndex === event.currentIndex) {
      return; // Không thay đổi gì
    }

    // Cập nhật UI cục bộ trước (Optimistic Update) để UX mượt mà
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }

    // Gửi lệnh di chuyển tới server qua Facade → API
    const task = event.container.data[event.currentIndex];
    this.boardFacade.moveTask(task.id, {
      targetColumnId: event.container.id,
      newPosition: event.currentIndex
    }).subscribe();
  }

  /** Xóa cột sau khi xác nhận */
  deleteColumn() {
    if (confirm('Xóa cột này?')) {
      this.boardFacade.deleteColumn(this.column().id).subscribe();
    }
  }

  /** Mở dialog tạo task mới */
  openTaskDialog() {
    this.showTaskDialog.set(true);
  }
}
