import { Component, OnInit, OnDestroy, signal, inject, input } from '@angular/core';
import { CdkDropListGroup } from '@angular/cdk/drag-drop';
import { BoardFacade } from '../../application/facades/board.facade';
import { BoardStateService } from '../../application/state/board-state.service';
import { ColumnComponent } from '../column/column.component';
import { ActivityFeedComponent } from '../activity-feed/activity-feed.component';

/**
 * ╔══════════════════════════════════════════════════════════════════════╗
 * ║  Board Detail Component - Trang chi tiết bảng Kanban               ║
 * ╠══════════════════════════════════════════════════════════════════════╣
 * ║  Luồng dữ liệu real-time end-to-end:                              ║
 * ║  1. Component gọi facade.loadBoard(id) → API trả về board data    ║
 * ║  2. Component gọi facade.connectRealtime(id) → mở 3 WebSocket     ║
 * ║  3. Facade subscribe SignalR events → cập nhật BoardStateService   ║
 * ║  4. Component đọc state.currentBoard() (signal) → Angular tự      ║
 * ║     re-render khi signal thay đổi (không cần manual subscribe)     ║
 * ║                                                                    ║
 * ║  CdkDropListGroup: Directive từ @angular/cdk cho phép drag-drop   ║
 * ║  task giữa nhiều column (nhiều danh sách). Mỗi column là 1        ║
 * ║  cdkDropList, được nhóm lại bằng cdkDropListGroup.                 ║
 * ╚══════════════════════════════════════════════════════════════════════╝
 */
@Component({
  selector: 'app-board-detail',
  standalone: true,
  imports: [CdkDropListGroup, ColumnComponent, ActivityFeedComponent],
  templateUrl: './board-detail.component.html',
  styleUrl: './board-detail.component.css'
})
export class BoardDetailComponent implements OnInit, OnDestroy {
  /**
   * input.required<string>(): Angular 16+ API nhận tham số từ route.
   * Khi route là /boards/:id, Angular tự bind giá trị :id vào input này
   * nhờ cấu hình withComponentInputBinding() trong app.config.ts.
   */
  id = input.required<string>();

  /** Facade điều phối: gọi API, kết nối SignalR, quản lý state */
  private boardFacade = inject(BoardFacade);
  /** State service chứa dữ liệu board hiện tại (signal-based) */
  boardState = inject(BoardStateService);

  /** Toggle hiển thị/ẩn sidebar Activity Feed */
  showActivities = signal(false);

  ngOnInit() {
    const boardId = this.id();
    // Bước 1: Load dữ liệu board từ REST API
    this.boardFacade.loadBoard(boardId);
    // Bước 2: Mở kết nối real-time để nhận push updates
    this.boardFacade.connectRealtime(boardId);
  }

  ngOnDestroy() {
    // Cleanup: ngắt SignalR khi rời trang để giải phóng tài nguyên
    this.boardFacade.disconnectRealtime(this.id());
  }

  /** Thêm cột mới vào board */
  addColumn() {
    const name = prompt('Nhập tên cột mới:');
    if (name) {
      this.boardFacade.createColumn(this.id(), { name }).subscribe();
    }
  }

  /** Toggle sidebar hiển thị activity feed */
  toggleActivities() {
    this.showActivities.update(v => !v);
  }
}
