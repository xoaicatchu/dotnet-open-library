export interface BoardDto {
  id: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
}

export interface BoardDetailDto extends BoardDto {
  columns: ColumnWithTasksDto[];
}

export interface ColumnDto {
  id: string;
  boardId: string;
  name: string;
  position: number;
  color: string;
}

export interface ColumnWithTasksDto extends ColumnDto {
  tasks: TaskDto[];
}

export interface TaskDto {
  id: string;
  columnId: string;
  title: string;
  description?: string;
  priority: 'Low' | 'Medium' | 'High' | 'Critical';
  assignee?: string;
  position: number;
  labels: string[];
  dueDate?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ActivityDto {
  id: string;
  boardId: string;
  actorName: string;
  actionType: 'Created' | 'Updated' | 'Deleted' | 'Moved';
  entityType: string;
  entityId: string;
  oldValue?: string;
  newValue?: string;
  timestamp: string;
}

export interface CreateBoardRequest {
  name: string;
  description?: string;
}

export interface UpdateBoardRequest {
  name: string;
  description?: string;
}

export interface CreateColumnRequest {
  name: string;
  color?: string;
}

export interface UpdateColumnRequest {
  name: string;
  color?: string;
}

export interface ReorderColumnsRequest {
  columnIds: string[];
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
  priority?: string;
  assignee?: string;
  labels?: string[];
  dueDate?: string;
}

export interface UpdateTaskRequest {
  title?: string;
  description?: string;
  priority?: string;
  assignee?: string;
  labels?: string[];
  dueDate?: string;
}

export interface MoveTaskRequest {
  targetColumnId: string;
  newPosition: number;
}
