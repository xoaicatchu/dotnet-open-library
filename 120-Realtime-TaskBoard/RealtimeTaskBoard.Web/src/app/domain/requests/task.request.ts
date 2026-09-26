import { TaskPriority } from '../models/task.model';

export interface CreateTaskRequest {
  title: string;
  description?: string;
  priority?: TaskPriority;
  assignee?: string;
  labels?: string[];
  dueDate?: Date | string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  priority?: TaskPriority;
  assignee?: string;
  labels?: string[];
  dueDate?: Date | string;
}

export interface MoveTaskRequest {
  targetColumnId: string;
  newPosition: number;
}
