import { Component, OnInit, OnDestroy, signal, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDropListGroup } from '@angular/cdk/drag-drop';
import { BoardFacade } from '../../application/facades/board.facade';
import { BoardStateService } from '../../application/state/board-state.service';
import { ColumnComponent } from '../column/column.component';
import { ActivityFeedComponent } from '../activity-feed/activity-feed.component';

@Component({
  selector: 'app-board-detail',
  standalone: true,
  imports: [CommonModule, CdkDropListGroup, ColumnComponent, ActivityFeedComponent],
  templateUrl: './board-detail.component.html',
  styleUrl: './board-detail.component.css'
})
export class BoardDetailComponent implements OnInit, OnDestroy {
  // input.required<string>() id: Angular 16+ input binding từ route params (bindToComponentInputs)
  id = input.required<string>();

  private boardFacade = inject(BoardFacade);
  boardState = inject(BoardStateService);

  showActivities = signal(false);

  /*
    Luồng dữ liệu real-time end-to-end:
    1. Component gọi facade.connectRealtime(boardId)
    2. Facade gọi signalR.connect() → mở 3 WebSocket tới 3 Hub
    3. Facade subscribe vào signalR events → cập nhật BoardStateService
    4. Component đọc state.currentBoard() (signal) → Angular tự re-render
  */
  ngOnInit() {
    this.boardFacade.loadBoard(this.id());
    this.boardFacade.connectRealtime(this.id());
  }

  ngOnDestroy() {
    this.boardFacade.disconnectRealtime();
  }

  addColumn() {
    const name = prompt('Nhập tên cột mới:');
    if (name) {
      this.boardFacade.createColumn(this.id(), name).subscribe();
    }
  }

  toggleActivities() {
    this.showActivities.update(v => !v);
  }
}
