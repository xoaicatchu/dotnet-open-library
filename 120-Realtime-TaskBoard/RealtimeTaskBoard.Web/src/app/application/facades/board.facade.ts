import { Injectable, inject, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import { IBoardApiPort } from '../../domain/ports/board-api.port';
import { ISignalRPort } from '../../domain/ports/signalr.port';
import { BoardStateService } from '../state/board-state.service';
import { CreateBoardRequest, UpdateBoardRequest } from '../../domain/requests/board.request';
import { CreateColumnRequest } from '../../domain/requests/column.request';
import { CreateTaskRequest, UpdateTaskRequest, MoveTaskRequest } from '../../domain/requests/task.request';

/**
 * Facade Pattern là lớp trung gian giúp "che giấu" sự phức tạp bên dưới.
 * Components không cần biết về HTTP Requests, SignalR, hay cách cập nhật state cục bộ.
 * Component chỉ việc gọi hàm của Facade và lắng nghe state.
 * 
 * Tại sao inject các abstract classes (Interfaces/Ports) thay vì inject adapter cụ thể?
 * Để tuân thủ Dependency Inversion. Facade chỉ tương tác thông qua Port (Hợp đồng),
 * nhờ đó có thể dễ dàng thay đổi thư viện gọi API hoặc mock test mà Facade không bị ảnh hưởng.
 */
@Injectable({ providedIn: 'root' })
export class BoardFacade implements OnDestroy {
  // Tiêm (inject) các phụ thuộc theo chuẩn Dependency Injection
  private readonly boardApi = inject(IBoardApiPort);
  private readonly signalR = inject(ISignalRPort);
  private readonly boardState = inject(BoardStateService);

  private signalRSubscriptions = new Subscription();

  /**
   * Lấy dữ liệu toàn bộ Board và danh sách log ban đầu từ API (REST),
   * sau đó đẩy dữ liệu vào State quản lý.
   */
  loadBoard(id: string) {
    this.boardApi.getBoard(id).subscribe(boardDetail => {
      this.boardState.setBoard(boardDetail);
    });

    this.boardApi.getActivities(id).subscribe(activities => {
      // Đặt danh sách activity (giả sử có phương thức setActivities, ở đây dùng tạm set qua update,
      // hoặc thêm vào state tùy logic)
      this.boardState.activities.set(activities);
    });
  }

  /**
   * Kết nối vào SignalR để nhận push notifications.
   * 
   * Tại sao subscribe events ở đây thay vì Component?
   * Vì Facade quản lý luồng dữ liệu trung tâm. Khi server đẩy (push) data về, 
   * Facade trực tiếp sửa đổi State. Các Component đang render UI dựa trên State sẽ tự động cập nhật
   * (Reactiveness). Logic đồng bộ data tập trung tại một nơi giúp dễ debug, dễ đọc.
   */
  async connectRealtime(boardId: string) {
    await this.signalR.connect(boardId);

    // Luồng dữ liệu SignalR end-to-end: 
    // User B thao tác -> Gọi API qua Facade -> API Server lưu DB -> Server broadcast qua Hub -> 
    // Client (User A) nhận event ở đây -> Facade update State cục bộ -> UI User A tự cập nhật qua Signal.
    this.signalRSubscriptions.add(
      this.signalR.boardEvents$.subscribe(event => {
        console.log('Realtime Board Event Received:', event);
        // Tùy theo event.type để gọi các hàm update tương ứng trên boardState
        // Ví dụ: if (event.type === 'ColumnCreated') this.boardState.addColumn(event.data);
      })
    );

    this.signalRSubscriptions.add(
      this.signalR.taskEvents$.subscribe(event => {
        console.log('Realtime Task Event Received:', event);
        // if (event.type === 'TaskCreated') this.boardState.addTask(event.data);
      })
    );

    this.signalRSubscriptions.add(
      this.signalR.activityEvents$.subscribe(event => {
        console.log('Realtime Activity Event Received:', event);
        // if (event.type === 'ActivityCreated') this.boardState.addActivity(event.data);
      })
    );
  }

  /**
   * Ngắt kết nối và giải phóng resources khi không cần thiết (khi thoát khỏi màn hình board).
   */
  async disconnectRealtime(boardId: string) {
    this.signalRSubscriptions.unsubscribe();
    await this.signalR.disconnect(boardId);
  }

  // ---- Các thao tác ghi qua API ----
  
  createBoard(req: CreateBoardRequest) {
    return this.boardApi.createBoard(req).subscribe();
  }

  deleteBoard(id: string) {
    return this.boardApi.deleteBoard(id).subscribe();
  }

  createColumn(boardId: string, req: CreateColumnRequest) {
    return this.boardApi.createColumn(boardId, req).subscribe();
  }

  deleteColumn(id: string) {
    return this.boardApi.deleteColumn(id).subscribe();
  }

  createTask(columnId: string, req: CreateTaskRequest) {
    return this.boardApi.createTask(columnId, req).subscribe();
  }

  updateTask(id: string, req: UpdateTaskRequest) {
    return this.boardApi.updateTask(id, req).subscribe();
  }

  moveTask(id: string, req: MoveTaskRequest) {
    return this.boardApi.moveTask(id, req).subscribe();
  }

  deleteTask(id: string) {
    return this.boardApi.deleteTask(id).subscribe();
  }

  ngOnDestroy() {
    this.signalRSubscriptions.unsubscribe();
  }
}
