import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { IBoardApiPort } from '../../domain/ports/board-api.port';
import { Observable } from 'rxjs';

/**
 * Adapter Pattern: class này triển khai 'hợp đồng' IBoardApiPort bằng HttpClient thực tế
 * Tại sao không dùng providedIn: 'root' mà để DI binding riêng → để có thể swap sang MockApiAdapter khi unit test
 */
@Injectable()
export class BoardApiAdapter extends IBoardApiPort {
  /**
   * Proxy configuration: URL bắt đầu bằng /api sẽ được Angular dev server proxy tới http://localhost:5155 (cấu hình trong proxy.conf.json)
   */
  private baseUrl = '/api';
  private http = inject(HttpClient);

  // --- Boards ---
  getBoards(): Observable<any> {
    return this.http.get(`${this.baseUrl}/boards`);
  }

  getBoard(id: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/boards/${id}`);
  }

  createBoard(req: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/boards`, req);
  }

  updateBoard(id: string, req: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/boards/${id}`, req);
  }

  deleteBoard(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/boards/${id}`);
  }

  // --- Columns ---
  getColumns(boardId: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/boards/${boardId}/columns`);
  }

  createColumn(boardId: string, req: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/boards/${boardId}/columns`, req);
  }

  updateColumn(id: string, req: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/columns/${id}`, req);
  }

  reorderColumns(req: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/columns/reorder`, req);
  }

  deleteColumn(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/columns/${id}`);
  }

  // --- Tasks ---
  getTasks(columnId: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/columns/${columnId}/tasks`);
  }

  createTask(columnId: string, req: any): Observable<any> {
    return this.http.post(`${this.baseUrl}/columns/${columnId}/tasks`, req);
  }

  updateTask(id: string, req: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/tasks/${id}`, req);
  }

  moveTask(id: string, req: any): Observable<any> {
    return this.http.put(`${this.baseUrl}/tasks/${id}/move`, req);
  }

  deleteTask(id: string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/tasks/${id}`);
  }

  // --- Activities ---
  getActivities(boardId: string): Observable<any> {
    return this.http.get(`${this.baseUrl}/boards/${boardId}/activities?limit=50`);
  }
}
