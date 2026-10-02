import { Injectable, inject, OnDestroy } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { IBoardApiPort } from '../../domain/ports/board-api.port';
import { ISignalRPort } from '../../domain/ports/signalr.port';
import { BoardStateService } from '../state/board-state.service';
import { BoardDto } from '../../domain/models/board.model';
import { CreateBoardRequest, UpdateBoardRequest } from '../../domain/requests/board.request';
import { CreateColumnRequest } from '../../domain/requests/column.request';
import { CreateTaskRequest, UpdateTaskRequest, MoveTaskRequest } from '../../domain/requests/task.request';

/**
 * ╔══════════════════════════════════════════════════════════════════════╗
 * ║  TẦNG APPLICATION - Board Facade (Điều phối Use-case)              ║
 * ╠══════════════════════════════════════════════════════════════════════╣
 * ║  Facade Pattern: Lớp trung gian giữa Presentation và các service  ║
 * ║  phức tạp bên dưới. Component chỉ cần gọi Facade, không cần biết  ║
 * ║  về HTTP, SignalR, hay cách quản lý state cục bộ.                  ║
 * ║                                                                    ║
 * ║  Luồng dữ liệu SignalR end-to-end:                                ║
 * ║  User B thao tác → Facade gọi API → Server lưu DB →               ║
 * ║  Server broadcast qua Hub → Client (User A) nhận event ở đây →    ║
 * ║  Facade cập nhật State → Angular tự re-render UI qua Signal.       ║
 * ║                                                                    ║
 * ║  Tại sao inject abstract class (Port) thay vì adapter cụ thể?     ║
 * ║  → Dependency Inversion: Facade chỉ tương tác qua hợp đồng,      ║
 * ║  có thể swap HTTP client thành mock khi test mà không sửa code.   ║
 * ╚══════════════════════════════════════════════════════════════════════╝
 */
@Injectable({ providedIn: 'root' })
export class BoardFacade implements OnDestroy {
  /** Inject các phụ thuộc qua abstract Port (Dependency Inversion) */
  private readonly boardApi = inject(IBoardApiPort);
  private readonly signalR = inject(ISignalRPort);
  readonly state = inject(BoardStateService);

  /** Quản lý tất cả subscription SignalR để cleanup khi disconnect */
  private signalRSubs = new Subscription();

  // ═══════════════════════════════════════════
  //  QUERY: Đọc dữ liệu từ API
  // ═══════════════════════════════════════════

  /** Lấy danh sách tất cả board (cho trang board-list) */
  getBoards(): Observable<BoardDto[]> {
    return this.boardApi.getBoards();
  }

  /** Load chi tiết 1 board + activities → đẩy vào State */
  loadBoard(id: string) {
    this.boardApi.getBoard(id).subscribe(board => {
      // Sắp xếp cột và task theo position trước khi set vào state
      board.columns.sort((a, b) => a.position - b.position);
      board.columns.forEach(c => c.tasks.sort((a, b) => a.position - b.position));
      this.state.setBoard(board);
    });

    this.boardApi.getActivities(id).subscribe(acts => {
      this.state.activities.set(acts);
    });
  }

  // ═══════════════════════════════════════════
  //  REALTIME: Kết nối SignalR
  // ═══════════════════════════════════════════

  /**
   * Kết nối vào 3 SignalR Hub và subscribe events.
   *
   * Tại sao subscribe events ở Facade thay vì Component?
   * → Vì Facade quản lý luồng dữ liệu trung tâm. Khi server push data,
   * Facade trực tiếp sửa State. Components đang render UI dựa trên State
   * sẽ tự động cập nhật nhờ Angular Signal reactivity.
   * Logic đồng bộ data tập trung 1 nơi → dễ debug, dễ maintain.
   */
  async connectRealtime(boardId: string) {
    await this.signalR.connect(boardId);

    // Subscribe board events: BoardUpdated, ColumnAdded, ColumnMoved, ColumnDeleted
    this.signalRSubs.add(
      this.signalR.boardEvents$.subscribe(event => {
        if (event.type === 'BoardUpdated') this.state.updateBoardInfo(event.data);
        if (event.type === 'ColumnAdded') this.state.addColumn(event.data);
        if (event.type === 'ColumnDeleted') this.state.removeColumn(event.data.columnId ?? event.data);
        if (event.type === 'ColumnMoved') this.loadBoard(boardId); // Reload khi reorder
      })
    );

    // Subscribe task events: TaskCreated, TaskUpdated, TaskMoved, TaskDeleted
    this.signalRSubs.add(
      this.signalR.taskEvents$.subscribe(event => {
        if (event.type === 'TaskCreated') this.state.addTask(event.data);
        if (event.type === 'TaskUpdated') this.state.updateTask(event.data);
        if (event.type === 'TaskDeleted') this.state.removeTask(event.data.taskId, event.data.columnId);
        if (event.type === 'TaskMoved') this.loadBoard(boardId);
      })
    );

    // Subscribe activity events: ActivityLogged
    this.signalRSubs.add(
      this.signalR.activityEvents$.subscribe(event => {
        if (event.type === 'ActivityLogged') this.state.addActivity(event.data);
      })
    );
  }

  /** Ngắt kết nối SignalR và cleanup tất cả subscriptions */
  async disconnectRealtime(boardId: string) {
    this.signalRSubs.unsubscribe();
    this.signalRSubs = new Subscription(); // Reset để có thể reconnect
    await this.signalR.disconnect(boardId);
  }

  // ═══════════════════════════════════════════
  //  COMMAND: Ghi dữ liệu qua API
  // ═══════════════════════════════════════════

  /** Tạo board mới */
  createBoard(req: CreateBoardRequest): Observable<BoardDto> {
    return this.boardApi.createBoard(req);
  }

  /** Xóa board */
  deleteBoard(id: string) {
    return this.boardApi.deleteBoard(id);
  }

  /** Tạo cột mới trong board */
  createColumn(boardId: string, req: CreateColumnRequest) {
    return this.boardApi.createColumn(boardId, req);
  }

  /** Xóa cột */
  deleteColumn(id: string) {
    return this.boardApi.deleteColumn(id);
  }

  /** Tạo task mới trong cột */
  createTask(columnId: string, req: CreateTaskRequest) {
    return this.boardApi.createTask(columnId, req);
  }

  /** Cập nhật task */
  updateTask(id: string, req: UpdateTaskRequest) {
    return this.boardApi.updateTask(id, req);
  }

  /** Di chuyển task sang cột/vị trí khác */
  moveTask(id: string, req: MoveTaskRequest) {
    return this.boardApi.moveTask(id, req);
  }

  /** Xóa task */
  deleteTask(id: string) {
    return this.boardApi.deleteTask(id);
  }

  ngOnDestroy() {
    this.signalRSubs.unsubscribe();
  }
}
