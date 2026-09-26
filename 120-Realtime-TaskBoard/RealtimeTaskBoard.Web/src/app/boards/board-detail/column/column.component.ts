import { Component, input, inject, signal } from '@angular/core';
import { ColumnWithTasksDto } from '../../../models/board.models';
import { CdkDragDrop, CdkDropList, CdkDrag, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { TaskCardComponent } from '../task-card/task-card.component';
import { ApiService } from '../../../services/api.service';
import { TaskDialogComponent } from '../task-dialog/task-dialog.component';

@Component({
  selector: 'app-column',
  standalone: true,
  imports: [CdkDropList, CdkDrag, TaskCardComponent, TaskDialogComponent],
  template: `
    <div class="column">
      <div class="column-header" [style.borderTopColor]="column().color || 'var(--primary-color)'">
        <h3>{{ column().name }} <span class="count">{{ column().tasks.length }}</span></h3>
        <button class="btn-icon" (click)="deleteColumn()">×</button>
      </div>

      <div 
        class="task-list"
        cdkDropList
        [id]="column().id"
        [cdkDropListData]="column().tasks"
        (cdkDropListDropped)="drop($event)">
        
        @for (task of column().tasks; track task.id) {
          <app-task-card [task]="task" cdkDrag></app-task-card>
        }
      </div>

      <div class="column-footer">
        <button class="btn-add" (click)="openTaskDialog()">+ Add a card</button>
      </div>

      @if (showTaskDialog()) {
        <app-task-dialog 
          [columnId]="column().id" 
          (close)="showTaskDialog.set(false)">
        </app-task-dialog>
      }
    </div>
  `,
  styles: [`
    .column {
      background: var(--column-bg);
      border-radius: 8px;
      width: 300px;
      min-width: 300px;
      max-height: 100%;
      display: flex;
      flex-direction: column;
      box-shadow: var(--shadow-sm);
    }
    .column-header {
      padding: 1rem;
      border-top: 4px solid var(--primary-color);
      border-radius: 8px 8px 0 0;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .column-header h3 {
      font-size: 1rem;
      margin: 0;
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }
    .count {
      background: var(--border-color);
      padding: 2px 8px;
      border-radius: 12px;
      font-size: 0.75rem;
    }
    .btn-icon {
      background: transparent;
      border: none;
      font-size: 1.25rem;
      cursor: pointer;
      color: var(--text-secondary);
    }
    .task-list {
      padding: 0.5rem;
      flex: 1;
      overflow-y: auto;
      min-height: 200px;
      border-radius: 6px;
      transition: background-color 0.15s ease;
    }
    .task-list.cdk-drop-list-dragging {
      background: rgba(0, 82, 204, 0.05);
    }
    .column-footer {
      padding: 0.5rem 1rem 1rem;
    }
    .btn-add {
      background: transparent;
      border: none;
      color: var(--text-secondary);
      cursor: pointer;
      width: 100%;
      text-align: left;
      padding: 0.5rem;
      border-radius: 4px;
    }
    .btn-add:hover { background: rgba(9, 30, 66, 0.08); }
  `]
})
export class ColumnComponent {
  column = input.required<ColumnWithTasksDto>();
  private api = inject(ApiService);
  
  showTaskDialog = signal(false);

  drop(event: CdkDragDrop<any[]>) {
    if (event.previousContainer === event.container && event.previousIndex === event.currentIndex) {
      return;
    }

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
    
    const task = event.container.data[event.currentIndex];
    const targetColumnId = event.container.id;
    const newPosition = event.currentIndex;

    this.api.moveTask(task.id, { targetColumnId, newPosition }).subscribe();
  }

  deleteColumn() {
    if (confirm('Delete this column?')) {
      this.api.deleteColumn(this.column().id).subscribe();
    }
  }

  openTaskDialog() {
    this.showTaskDialog.set(true);
  }
}
