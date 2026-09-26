import { Component, input, output, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, CdkDropList, CdkDrag } from '@angular/cdk/drag-drop';
import { BoardFacade } from '../../application/facades/board.facade';
import { ColumnWithTasksDto } from '../../domain/models/column.model';
import { TaskDto } from '../../domain/models/task.model';
import { TaskCardComponent } from '../task-card/task-card.component';
import { TaskDialogComponent } from '../task-dialog/task-dialog.component';

@Component({
  selector: 'app-column',
  standalone: true,
  imports: [CommonModule, CdkDropList, CdkDrag, TaskCardComponent, TaskDialogComponent],
  templateUrl: './column.component.html',
  styleUrl: './column.component.css'
})
export class ColumnComponent {
  column = input.required<ColumnWithTasksDto>();
  taskDropped = output<CdkDragDrop<TaskDto[]>>();
  columnDeleted = output<string>();

  private boardFacade = inject(BoardFacade);
  showTaskDialog = signal(false);

  /*
    - CdkDropList: directive biến div thành vùng nhận kéo thả
    - cdkDropListData: bind dữ liệu array để CDK quản lý thứ tự
    - CdkDragDrop event: chứa previousContainer, container, previousIndex, currentIndex
    - moveItemInArray vs transferArrayItem: di chuyển trong cùng list vs giữa 2 list
  */
  drop(event: CdkDragDrop<TaskDto[]>) {
    if (event.previousContainer === event.container) {
      if (event.previousIndex !== event.currentIndex) {
        // Gọi facade xử lý di chuyển trong cùng cột
        this.boardFacade.moveTask(event.item.data.id, this.column().id, event.currentIndex).subscribe();
      }
    } else {
      // Gọi facade xử lý kéo thả sang cột khác
      this.boardFacade.moveTask(event.item.data.id, this.column().id, event.currentIndex).subscribe();
    }
  }

  deleteColumn() {
    if (confirm('Bạn có chắc muốn xóa cột này không?')) {
      this.boardFacade.deleteColumn(this.column().id).subscribe(() => {
        this.columnDeleted.emit(this.column().id);
      });
    }
  }
}
