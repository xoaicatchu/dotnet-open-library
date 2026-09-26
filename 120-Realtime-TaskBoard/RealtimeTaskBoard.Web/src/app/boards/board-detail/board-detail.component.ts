import { Component, inject, input, OnInit, OnDestroy, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { BoardService } from '../../services/board.service';
import { SignalRService } from '../../services/signalr.service';
import { ApiService } from '../../services/api.service';
import { CdkDropListGroup } from '@angular/cdk/drag-drop';
import { ColumnComponent } from './column/column.component';
import { ActivityFeedComponent } from './activity-feed/activity-feed.component';

@Component({
  selector: 'app-board-detail',
  standalone: true,
  imports: [CdkDropListGroup, ColumnComponent, ActivityFeedComponent],
  template: `
    @if (boardService.currentBoard(); as board) {
      <div class="board-layout">
        <div class="board-header">
          <h2>{{ board.name }}</h2>
          <div class="actions">
            <button class="btn btn-primary" (click)="addColumn()">Add Column</button>
            <button class="btn" (click)="toggleActivities()">Activities</button>
          </div>
        </div>

        <div class="board-content" [class.with-sidebar]="showActivities()">
          <div class="columns-container" cdkDropListGroup>
            @for (column of board.columns; track column.id) {
              <app-column [column]="column"></app-column>
            }
          </div>

          @if (showActivities()) {
            <div class="sidebar">
              <app-activity-feed></app-activity-feed>
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    .board-layout { display: flex; flex-direction: column; height: 100%; }
    .board-header { 
      padding: 1rem 2rem; 
      display: flex; 
      justify-content: space-between; 
      align-items: center;
      background: var(--card-bg);
      border-bottom: 1px solid var(--border-color);
    }
    .board-content {
      display: flex;
      flex: 1;
      overflow: hidden;
      position: relative;
    }
    .columns-container {
      display: flex;
      padding: 1.5rem;
      gap: 1.5rem;
      overflow-x: auto;
      flex: 1;
      align-items: flex-start;
    }
    .sidebar {
      width: 300px;
      border-left: 1px solid var(--border-color);
      background: var(--card-bg);
      overflow-y: auto;
    }
    .actions { display: flex; gap: 0.5rem; }
  `]
})
export class BoardDetailComponent implements OnInit, OnDestroy {
  id = input.required<string>();
  
  boardService = inject(BoardService);
  signalR = inject(SignalRService);
  api = inject(ApiService);

  showActivities = signal(false);
  private subs = new Subscription();

  async ngOnInit() {
    const boardId = this.id();
    this.boardService.loadBoard(boardId);

    this.subs.add(
      this.signalR.boardEvents$.subscribe(bEv => {
        if (bEv.type === 'BoardUpdated') this.boardService.updateBoardState(bEv.data);
        if (bEv.type === 'ColumnAdded') this.boardService.addColumn(bEv.data);
        if (bEv.type === 'ColumnDeleted') this.boardService.removeColumn(bEv.data.columnId);
        if (bEv.type === 'ColumnMoved') this.boardService.loadBoard(this.id());
      })
    );

    this.subs.add(
      this.signalR.taskEvents$.subscribe(tEv => {
        if (tEv.type === 'TaskCreated') this.boardService.addTask(tEv.data);
        if (tEv.type === 'TaskUpdated') this.boardService.updateTask(tEv.data);
        if (tEv.type === 'TaskDeleted') this.boardService.removeTask(tEv.data.taskId, tEv.data.columnId);
        if (tEv.type === 'TaskMoved') this.boardService.moveTask(tEv.data);
      })
    );

    this.subs.add(
      this.signalR.activityEvents$.subscribe(aEv => {
        if (aEv?.type === 'ActivityLogged') {
          this.boardService.addActivity(aEv.data);
        }
      })
    );

    await this.signalR.connect(boardId);
  }

  async ngOnDestroy() {
    this.subs.unsubscribe();
    await this.signalR.disconnect(this.id());
  }

  toggleActivities() {
    this.showActivities.update(v => !v);
  }

  addColumn() {
    const name = prompt('Column name:');
    if (name) {
      this.api.createColumn(this.id(), { name }).subscribe();
    }
  }
}
