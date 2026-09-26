import { Component, OnInit, input, output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { BoardFacade } from '../../application/facades/board.facade';
import { TaskDto } from '../../domain/models/task.model';

@Component({
  selector: 'app-task-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './task-dialog.component.html',
  styleUrl: './task-dialog.component.css'
})
export class TaskDialogComponent implements OnInit {
  columnId = input.required<string>();
  task = input<TaskDto>(); // optional cho edit mode
  close = output<void>();

  private boardFacade = inject(BoardFacade);
  private fb = inject(FormBuilder);

  /*
    - ReactiveFormsModule: Angular forms API cho phép tạo form programmatically
    - FormGroup/FormControl: quản lý giá trị, validation, dirty/touched state
  */
  taskForm = this.fb.group({
    title: ['', Validators.required],
    description: [''],
    priority: ['Medium', Validators.required],
    assignee: ['']
  });

  ngOnInit() {
    const t = this.task();
    if (t) {
      this.taskForm.patchValue({
        title: t.title,
        description: t.description,
        priority: t.priority,
        assignee: t.assignee
      });
    }
  }

  save() {
    if (this.taskForm.valid) {
      const formValue = this.taskForm.value;
      const t = this.task();
      
      if (t) {
        // Cập nhật task hiện có
        this.boardFacade.updateTask(t.id, {
          title: formValue.title!,
          description: formValue.description || undefined,
          priority: formValue.priority as any,
          assignee: formValue.assignee || undefined
        }).subscribe(() => this.close.emit());
      } else {
        // Tạo task mới
        this.boardFacade.createTask(this.columnId(), {
          title: formValue.title!,
          description: formValue.description || undefined,
          priority: formValue.priority as any,
          assignee: formValue.assignee || undefined
        }).subscribe(() => this.close.emit());
      }
    }
  }

  onBackdropClick() {
    // Dialog pattern: backdrop click đóng dialog
    this.close.emit();
  }

  onDialogClick(event: Event) {
    // Dialog pattern: stopPropagation ngăn event bubble
    event.stopPropagation();
  }
}
