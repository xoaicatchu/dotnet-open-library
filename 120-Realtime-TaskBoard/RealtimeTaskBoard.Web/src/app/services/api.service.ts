import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { 
  BoardDto, BoardDetailDto, CreateBoardRequest, UpdateBoardRequest,
  ColumnDto, CreateColumnRequest, UpdateColumnRequest, ReorderColumnsRequest,
  TaskDto, CreateTaskRequest, UpdateTaskRequest, MoveTaskRequest, ActivityDto
} from '../models/board.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private baseUrl = '/api';

  // Boards
  getBoards(): Observable<BoardDto[]> {
    return this.http.get<BoardDto[]>(`${this.baseUrl}/boards`);
  }
  getBoard(id: string): Observable<BoardDetailDto> {
    return this.http.get<BoardDetailDto>(`${this.baseUrl}/boards/${id}`);
  }
  createBoard(req: CreateBoardRequest): Observable<BoardDto> {
    return this.http.post<BoardDto>(`${this.baseUrl}/boards`, req);
  }
  updateBoard(id: string, req: UpdateBoardRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/boards/${id}`, req);
  }
  deleteBoard(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/boards/${id}`);
  }

  // Columns
  getColumns(boardId: string): Observable<ColumnDto[]> {
    return this.http.get<ColumnDto[]>(`${this.baseUrl}/boards/${boardId}/columns`);
  }
  createColumn(boardId: string, req: CreateColumnRequest): Observable<ColumnDto> {
    return this.http.post<ColumnDto>(`${this.baseUrl}/boards/${boardId}/columns`, req);
  }
  updateColumn(id: string, req: UpdateColumnRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/columns/${id}`, req);
  }
  reorderColumns(req: ReorderColumnsRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/columns/reorder`, req);
  }
  deleteColumn(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/columns/${id}`);
  }

  // Tasks
  getTasks(columnId: string): Observable<TaskDto[]> {
    return this.http.get<TaskDto[]>(`${this.baseUrl}/columns/${columnId}/tasks`);
  }
  createTask(columnId: string, req: CreateTaskRequest): Observable<TaskDto> {
    return this.http.post<TaskDto>(`${this.baseUrl}/columns/${columnId}/tasks`, req);
  }
  updateTask(id: string, req: UpdateTaskRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/tasks/${id}`, req);
  }
  moveTask(id: string, req: MoveTaskRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/tasks/${id}/move`, req);
  }
  deleteTask(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/tasks/${id}`);
  }

  // Activities
  getActivities(boardId: string): Observable<ActivityDto[]> {
    return this.http.get<ActivityDto[]>(`${this.baseUrl}/boards/${boardId}/activities?limit=50`);
  }
}
