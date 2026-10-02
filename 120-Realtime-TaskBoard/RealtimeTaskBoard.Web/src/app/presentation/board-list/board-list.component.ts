import { Component, OnInit, signal, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BoardFacade } from '../../application/facades/board.facade';
import { BoardDto } from '../../domain/models/board.model';

/**
 * ╔══════════════════════════════════════════════════════════════════════╗
 * ║  Board List Component - Trang danh sách bảng Kanban                ║
 * ╠══════════════════════════════════════════════════════════════════════╣
 * ║  Hiển thị tất cả board dưới dạng grid card.                       ║
 * ║  Click vào card → navigate tới /boards/:id (trang board detail).   ║
 * ║                                                                    ║
 * ║  Sử dụng @for control flow (Angular 17+) thay cho *ngFor cũ.      ║
 * ║  @for có hiệu năng tốt hơn nhờ track expression tối ưu DOM diff.  ║
 * ╚══════════════════════════════════════════════════════════════════════╝
 */
@Component({
  selector: 'app-board-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './board-list.component.html',
  styleUrl: './board-list.component.css'
})
export class BoardListComponent implements OnInit {
  /** Facade là điểm truy cập duy nhất vào tầng Application */
  private boardFacade = inject(BoardFacade);

  /** Signal chứa danh sách board - Angular tự re-render khi signal thay đổi */
  boards = signal<BoardDto[]>([]);

  ngOnInit() {
    this.loadBoards();
  }

  /** Load danh sách boards từ API thông qua Facade */
  loadBoards() {
    this.boardFacade.getBoards().subscribe(
      (data: BoardDto[]) => this.boards.set(data)
    );
  }

  /** Tạo board mới bằng prompt đơn giản, sau đó reload danh sách */
  createNewBoard() {
    const name = prompt('Nhập tên Board mới:');
    if (name) {
      this.boardFacade.createBoard({ name }).subscribe(() => this.loadBoards());
    }
  }
}
