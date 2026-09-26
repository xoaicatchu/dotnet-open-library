import { Component, inject, OnInit, signal } from '@angular/core';
import { ApiService } from '../../services/api.service';
import { BoardDto } from '../../models/board.models';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-board-list',
  standalone: true,
  imports: [RouterLink, DatePipe],
  template: `
    <div class="container">
      <div class="header">
        <h1>Your Boards</h1>
        <button class="btn btn-primary" (click)="createNewBoard()">Create Board</button>
      </div>

      <div class="board-grid">
        @for (board of boards(); track board.id) {
          <a [routerLink]="['/boards', board.id]" class="board-card">
            <h3>{{ board.name }}</h3>
            <p>{{ board.description }}</p>
            <small>Updated: {{ board.updatedAt | date }}</small>
          </a>
        }
      </div>
    </div>
  `,
  styles: [`
    .container { padding: 2rem; }
    .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 2rem; }
    .board-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 1rem; }
    .board-card {
      background-color: var(--card-bg);
      border: 1px solid var(--border-color);
      border-radius: 8px;
      padding: 1.5rem;
      text-decoration: none;
      color: inherit;
      box-shadow: var(--shadow-sm);
      transition: transform 0.2s, box-shadow 0.2s;
    }
    .board-card:hover {
      transform: translateY(-2px);
      box-shadow: var(--shadow-md);
    }
    .board-card h3 { margin-bottom: 0.5rem; }
    .board-card p { color: var(--text-secondary); margin-bottom: 1rem; font-size: 0.875rem; }
  `]
})
export class BoardListComponent implements OnInit {
  private api = inject(ApiService);
  boards = signal<BoardDto[]>([]);

  ngOnInit() {
    this.loadBoards();
  }

  loadBoards() {
    this.api.getBoards().subscribe(data => this.boards.set(data));
  }

  createNewBoard() {
    const name = prompt('Enter board name:');
    if (name) {
      this.api.createBoard({ name }).subscribe(() => this.loadBoards());
    }
  }
}
