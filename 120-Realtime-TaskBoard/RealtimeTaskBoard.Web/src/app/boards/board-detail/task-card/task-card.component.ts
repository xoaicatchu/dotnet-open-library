import { Component, input, signal, inject } from '@angular/core';
import { TaskDto } from '../../../models/board.models';
import { TaskDialogComponent } from '../task-dialog/task-dialog.component';
import { ApiService } from '../../../services/api.service';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [TaskDialogComponent],
  template: `
    <div class="task-card" [class]="'priority-' + task().priority.toLowerCase()" (click)="editTask()">
      @if (task().labels.length) {
        <div class="task-labels">
          @for (label of task().labels; track label) {
            <span class="label">{{ label }}</span>
          }
        </div>
      }
      <div class="task-title">{{ task().title }}</div>
      
      <div class="task-footer">
        <span class="priority-badge">{{ task().priority }}</span>
        @if (task().assignee) {
          <span class="assignee">{{ task().assignee }}</span>
        }
      </div>
      <button class="delete-btn" (click)="deleteTask($event)">×</button>
    </div>

    @if (showEditDialog()) {
      <app-task-dialog 
        [columnId]="task().columnId" 
        [task]="task()" 
        (close)="showEditDialog.set(false)">
      </app-task-dialog>
    }
  `,
  styles: [`
    :host {
      display: block;
    }
    .task-card {
      background: var(--card-bg);
      border-radius: 4px;
      padding: 0.75rem;
      margin-bottom: 0.5rem;
      box-shadow: var(--shadow-sm);
      cursor: grab;
      position: relative;
      border-left: 4px solid transparent;
      user-select: none;
      -webkit-user-select: none;
    }
    .task-card:active { cursor: grabbing; }
    
    .priority-low { border-left-color: #00875a; }
    .priority-medium { border-left-color: #ff991f; }
    .priority-high { border-left-color: #de350b; }
    .priority-critical { border-left-color: #bf2600; }

    .task-title {
      font-size: 0.875rem;
      margin-bottom: 0.5rem;
      word-wrap: break-word;
    }
    .task-labels {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
      margin-bottom: 0.5rem;
    }
    .label {
      background: var(--primary-color);
      color: white;
      font-size: 0.65rem;
      padding: 2px 6px;
      border-radius: 3px;
    }
    .task-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 0.75rem;
      color: var(--text-secondary);
    }
    .priority-badge {
      font-size: 0.7rem;
      text-transform: uppercase;
      font-weight: bold;
    }
    .delete-btn {
      position: absolute;
      top: 4px;
      right: 4px;
      background: transparent;
      border: none;
      color: var(--text-secondary);
      cursor: pointer;
      display: none;
    }
    .task-card:hover .delete-btn { display: block; }
  `]
})
export class TaskCardComponent {
  task = input.required<TaskDto>();
  private api = inject(ApiService);
  
  showEditDialog = signal(false);

  editTask() {
    this.showEditDialog.set(true);
  }

  deleteTask(event: Event) {
    event.stopPropagation();
    if (confirm('Delete this task?')) {
      this.api.deleteTask(this.task().id).subscribe();
    }
  }
}
