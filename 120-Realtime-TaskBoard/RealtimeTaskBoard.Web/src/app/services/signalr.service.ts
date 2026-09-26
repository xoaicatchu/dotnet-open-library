import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private boardHub: signalR.HubConnection | null = null;
  private taskHub: signalR.HubConnection | null = null;
  private activityHub: signalR.HubConnection | null = null;

  boardEvents$ = new Subject<{ type: string; data: any }>();
  taskEvents$ = new Subject<{ type: string; data: any }>();
  activityEvents$ = new Subject<{ type: string; data: any }>();

  async connect(boardId: string) {
    this.boardHub = new signalR.HubConnectionBuilder().withUrl('/hubs/board').build();
    this.taskHub = new signalR.HubConnectionBuilder().withUrl('/hubs/task').build();
    this.activityHub = new signalR.HubConnectionBuilder().withUrl('/hubs/activity').build();

    this.registerEvents();

    try {
      await Promise.all([
        this.boardHub.start(),
        this.taskHub.start(),
        this.activityHub.start()
      ]);

      await Promise.all([
        this.boardHub.invoke('JoinBoard', boardId),
        this.taskHub.invoke('JoinBoard', boardId),
        this.activityHub.invoke('JoinBoard', boardId)
      ]);
    } catch (err) {
      console.error('Error connecting to SignalR', err);
    }
  }

  async disconnect(boardId: string) {
    try {
      if (this.boardHub?.state === signalR.HubConnectionState.Connected) {
        await this.boardHub.invoke('LeaveBoard', boardId);
        await this.boardHub.stop();
      }
      if (this.taskHub?.state === signalR.HubConnectionState.Connected) {
        await this.taskHub.invoke('LeaveBoard', boardId);
        await this.taskHub.stop();
      }
      if (this.activityHub?.state === signalR.HubConnectionState.Connected) {
        await this.activityHub.invoke('LeaveBoard', boardId);
        await this.activityHub.stop();
      }
    } catch (err) {
      console.error('Error disconnecting from SignalR', err);
    }
  }

  private registerEvents() {
    if (this.boardHub) {
      this.boardHub.on('BoardUpdated', (data) => this.boardEvents$.next({ type: 'BoardUpdated', data }));
      this.boardHub.on('ColumnAdded', (data) => this.boardEvents$.next({ type: 'ColumnAdded', data }));
      this.boardHub.on('ColumnMoved', (data) => this.boardEvents$.next({ type: 'ColumnMoved', data }));
      this.boardHub.on('ColumnDeleted', (data) => this.boardEvents$.next({ type: 'ColumnDeleted', data }));
    }

    if (this.taskHub) {
      this.taskHub.on('TaskCreated', (data) => this.taskEvents$.next({ type: 'TaskCreated', data }));
      this.taskHub.on('TaskMoved', (data) => this.taskEvents$.next({ type: 'TaskMoved', data }));
      this.taskHub.on('TaskUpdated', (data) => this.taskEvents$.next({ type: 'TaskUpdated', data }));
      this.taskHub.on('TaskDeleted', (data) => this.taskEvents$.next({ type: 'TaskDeleted', data }));
    }

    if (this.activityHub) {
      this.activityHub.on('ActivityLogged', (data) => this.activityEvents$.next({ type: 'ActivityLogged', data }));
    }
  }
}
