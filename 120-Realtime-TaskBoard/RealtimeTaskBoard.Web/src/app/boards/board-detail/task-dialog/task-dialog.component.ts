import { Component, input, output, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../../services/api.service';
import { TaskDto } from '../../../models/board.models';

@Component({
  selector: 'app-task-dialog',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="dialog-backdrop" (click)="closeDialog($event)">
      <div class="dialog-content" (click)="$event.stopPropagation()">
        <div class="dialog-header">
          <h2>{{ task() ? 'Edit Task' : 'New Task' }}</h2>
          <button class="btn" type="button" (click)="closeDialog($event)">×</button>
        </div>

        <form [formGroup]="form" (ngSubmit)="save($event)">
          <div class="form-group">
            <label>Title</label>
            <input type="text" class="form-control" formControlName="title" required>
          </div>

          <div class="form-group">
            <label>Description</label>
            <textarea class="form-control" formControlName="description" rows="3"></textarea>
          </div>

          <div class="form-group">
            <label>Priority</label>
            <select class="form-control" formControlName="priority">
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Critical">Critical</option>
            </select>
          </div>

          <div class="form-group">
            <label>Assignee</label>
            <input type="text" class="form-control" formControlName="assignee">
          </div>

          <div class="dialog-footer">
            <button type="button" class="btn" (click)="closeDialog($event)">Cancel</button>
            <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSaving()">
              {{ isSaving() ? 'Saving...' : 'Save' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class TaskDialogComponent implements OnInit {
  columnId = input.required<string>();
  task = input<TaskDto | null>(null);
  close = output<void>();

  private api = inject(ApiService);
  private fb = inject(FormBuilder);
  isSaving = signal(false);

  form = this.fb.group({
    title: ['', Validators.required],
    description: [''],
    priority: ['Medium'],
    assignee: ['']
  });

  ngOnInit() {
    const t = this.task();
    if (t) {
      this.form.patchValue(t);
    }
  }

  closeDialog(event?: Event) {
    event?.stopPropagation();
    this.close.emit();
  }

  save(event?: Event) {
    event?.preventDefault();
    event?.stopPropagation();
    if (this.form.invalid || this.isSaving()) return;

    this.isSaving.set(true);
    const t = this.task();
    const payload = this.form.value as any;

    if (t) {
      this.api.updateTask(t.id, payload).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.closeDialog();
        },
        error: (err) => {
          this.isSaving.set(false);
          console.error('Failed to update task', err);
        }
      });
    } else {
      this.api.createTask(this.columnId(), payload).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.closeDialog();
        },
        error: (err) => {
          this.isSaving.set(false);
          console.error('Failed to create task', err);
        }
      });
    }
  }
}
