import { Component, input, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BoardFacade } from '../../application/facades/board.facade';
import { TaskDto } from '../../domain/models/task.model';
import { TaskDialogComponent } from '../task-dialog/task-dialog.component';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [CommonModule, TaskDialogComponent],
  templateUrl: './task-card.component.html',
  styleUrl: './task-card.component.css'
})
export class TaskCardComponent {
  // TaskCard là component hiển thị 1 thẻ task trên bảng Kanban.
  task = input.required<TaskDto>();
  private boardFacade = inject(BoardFacade);
  
  showDialog = signal(false);

  editTask() {
    this.showDialog.set(true);
  }

  deleteTask(event: Event) {
    event.stopPropagation(); // Ngăn sự kiện click truyền lên card (tránh mở dialog)
    if (confirm('Bạn có chắc muốn xóa task này?')) {
      this.boardFacade.deleteTask(this.task().id).subscribe();
    }
  }
}
