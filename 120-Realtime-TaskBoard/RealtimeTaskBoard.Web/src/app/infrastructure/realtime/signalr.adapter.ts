import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import { ISignalRPort } from '../../domain/ports/signalr.port';

@Injectable()
export class SignalRAdapter implements ISignalRPort {
  // Khai báo 3 HubConnection: boardHub, taskHub, activityHub
  private boardHub: HubConnection | null = null;
  private taskHub: HubConnection | null = null;
  private activityHub: HubConnection | null = null;

  // Khai báo 3 Subject: boardEvents$, taskEvents$, activityEvents$ (public, kiểu Observable)
  private boardEventsSubject = new Subject<any>();
  public boardEvents$: Observable<any> = this.boardEventsSubject.asObservable();

  private taskEventsSubject = new Subject<any>();
  public taskEvents$: Observable<any> = this.taskEventsSubject.asObservable();

  private activityEventsSubject = new Subject<any>();
  public activityEvents$: Observable<any> = this.activityEventsSubject.asObservable();

  async connect(boardId: string): Promise<void> {
    // 1. Tạo HubConnection bằng HubConnectionBuilder().withUrl('/hubs/board').withAutomaticReconnect().build()
    // HubConnectionBuilder: Factory tạo kết nối SignalR, tương tự new WebSocket() nhưng cao cấp hơn
    // withUrl('/hubs/task'): URL endpoint của SignalR Hub trên server ASP.NET Core, được proxy qua Angular dev server
    // withAutomaticReconnect(): Tự động kết nối lại khi mất mạng, SignalR sẽ retry theo exponential backoff (0s, 2s, 10s, 30s)
    this.boardHub = new HubConnectionBuilder()
      .withUrl('/hubs/board')
      .withAutomaticReconnect()
      .build();

    this.taskHub = new HubConnectionBuilder()
      .withUrl('/hubs/task')
      .withAutomaticReconnect()
      .build();

    this.activityHub = new HubConnectionBuilder()
      .withUrl('/hubs/activity')
      .withAutomaticReconnect()
      .build();

    this.registerEvents();

    try {
      // 3. Gọi hub.start() để mở kết nối WebSocket
      // hub.start(): Thực hiện handshake: HTTP negotiate → chọn transport (WebSocket ưu tiên) → mở kết nối persistent
      await Promise.all([
        this.boardHub.start(),
        this.taskHub.start(),
        this.activityHub.start()
      ]);

      // 4. Gọi hub.invoke('JoinBoard', boardId) để tham gia nhóm SignalR group
      // hub.invoke('JoinBoard', boardId): Gọi method trên server Hub, server sẽ thêm ConnectionId vào Group 'board-{boardId}'
      // Group SignalR: Cơ chế phân nhóm kết nối. Khi server broadcast tới group, CHỈ các client trong nhóm đó nhận được tin.
      await Promise.all([
        this.boardHub.invoke('JoinBoard', boardId),
        this.taskHub.invoke('JoinBoard', boardId),
        this.activityHub.invoke('JoinBoard', boardId)
      ]);
    } catch (err) {
      console.error('Lỗi khi kết nối SignalR:', err);
    }
  }

  async disconnect(boardId: string): Promise<void> {
    try {
      // 1. Gọi hub.invoke('LeaveBoard', boardId) để rời nhóm
      if (this.boardHub?.state === 'Connected') await this.boardHub.invoke('LeaveBoard', boardId);
      if (this.taskHub?.state === 'Connected') await this.taskHub.invoke('LeaveBoard', boardId);
      if (this.activityHub?.state === 'Connected') await this.activityHub.invoke('LeaveBoard', boardId);

      // 2. Gọi hub.stop() để đóng kết nối WebSocket
      // Luôn gọi LeaveBoard trước stop() để server cleanup Group membership
      if (this.boardHub) await this.boardHub.stop();
      if (this.taskHub) await this.taskHub.stop();
      if (this.activityHub) await this.activityHub.stop();
    } catch (err) {
      console.error('Lỗi khi ngắt kết nối SignalR:', err);
    }
  }

  private registerEvents(): void {
    // 2. Đăng ký lắng nghe events bằng hub.on('EventName', callback)
    // hub.on('TaskCreated', callback): Đăng ký handler khi server gọi SendAsync('TaskCreated', data) tới group
    
    // BoardHub events: BoardUpdated, ColumnAdded, ColumnMoved, ColumnDeleted
    this.boardHub?.on('BoardUpdated', (data) => this.boardEventsSubject.next({ type: 'BoardUpdated', data }));
    this.boardHub?.on('ColumnAdded', (data) => this.boardEventsSubject.next({ type: 'ColumnAdded', data }));
    this.boardHub?.on('ColumnMoved', (data) => this.boardEventsSubject.next({ type: 'ColumnMoved', data }));
    this.boardHub?.on('ColumnDeleted', (data) => this.boardEventsSubject.next({ type: 'ColumnDeleted', data }));

    // TaskHub events: TaskCreated, TaskMoved, TaskUpdated, TaskDeleted
    this.taskHub?.on('TaskCreated', (data) => this.taskEventsSubject.next({ type: 'TaskCreated', data }));
    this.taskHub?.on('TaskMoved', (data) => this.taskEventsSubject.next({ type: 'TaskMoved', data }));
    this.taskHub?.on('TaskUpdated', (data) => this.taskEventsSubject.next({ type: 'TaskUpdated', data }));
    this.taskHub?.on('TaskDeleted', (data) => this.taskEventsSubject.next({ type: 'TaskDeleted', data }));

    // ActivityHub events: ActivityLogged
    // Mỗi event tương ứng 1 hành động CRUD trên server. Khi user A tạo task, server gọi SendAsync('TaskCreated', dto) 
    // → tất cả user B, C, D đang xem cùng board sẽ nhận được event này qua WebSocket.
    this.activityHub?.on('ActivityLogged', (data) => this.activityEventsSubject.next({ type: 'ActivityLogged', data }));
  }
}
