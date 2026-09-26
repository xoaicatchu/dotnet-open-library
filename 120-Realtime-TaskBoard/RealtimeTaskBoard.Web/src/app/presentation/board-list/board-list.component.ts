import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BoardFacade } from '../../application/facades/board.facade';
import { BoardDto } from '../../domain/models/board.model';

@Component({
  selector: 'app-board-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './board-list.component.html',
  styleUrl: './board-list.component.css'
})
export class BoardListComponent implements OnInit {
  // Trang danh sách board, sử dụng @for control flow (Angular 17+ thay thế *ngFor).
  private boardFacade = inject(BoardFacade);
  boards = signal<BoardDto[]>([]);

  ngOnInit() {
    // Gọi facade để load danh sách boards khi component khởi tạo
    this.boardFacade.loadBoards().subscribe(
      (data) => this.boards.set(data)
    );
  }

  createNewBoard() {
    const name = prompt('Nhập tên Board mới:');
    if (name) {
      this.boardFacade.createBoard(name).subscribe(
        (newBoard) => {
          this.boards.update(b => [...b, newBoard]);
        }
      );
    }
  }
}
