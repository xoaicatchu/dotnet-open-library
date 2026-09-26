import { Injectable, inject, signal } from '@angular/core';
import { ApiService } from './api.service';
import { BoardDetailDto, ColumnWithTasksDto, TaskDto, ActivityDto } from '../models/board.models';

@Injectable({ providedIn: 'root' })
export class BoardService {
  private api = inject(ApiService);

  currentBoard = signal<BoardDetailDto | null>(null);
  activities = signal<ActivityDto[]>([]);

  async loadBoard(id: string) {
    this.api.getBoard(id).subscribe(board => {
      // sort columns by position, and tasks by position
      board.columns.sort((a, b) => a.position - b.position);
      board.columns.forEach(c => c.tasks.sort((a, b) => a.position - b.position));
      this.currentBoard.set(board);
    });
    this.api.getActivities(id).subscribe(acts => {
      this.activities.set(acts);
    });
  }

  // Local state updaters based on SignalR events
  updateBoardState(boardUpdated: any) {
    const board = this.currentBoard();
    if (board && board.id === boardUpdated.id) {
      this.currentBoard.set({ ...board, name: boardUpdated.name, description: boardUpdated.description });
    }
  }

  addColumn(column: ColumnWithTasksDto) {
    const board = this.currentBoard();
    if (board) {
      const columns = [...board.columns, { ...column, tasks: [] }];
      this.currentBoard.set({ ...board, columns });
    }
  }

  removeColumn(columnId: string) {
    const board = this.currentBoard();
    if (board) {
      const columns = board.columns.filter(c => c.id !== columnId);
      this.currentBoard.set({ ...board, columns });
    }
  }

  addTask(task: TaskDto) {
    const board = this.currentBoard();
    if (board) {
      const columns = board.columns.map(c => {
        if (c.id === task.columnId) {
          return { ...c, tasks: [...c.tasks, task].sort((a, b) => a.position - b.position) };
        }
        return c;
      });
      this.currentBoard.set({ ...board, columns });
    }
  }

  updateTask(task: TaskDto) {
    const board = this.currentBoard();
    if (board) {
      const columns = board.columns.map(c => {
        if (c.id === task.columnId) {
          return { ...c, tasks: c.tasks.map(t => t.id === task.id ? task : t).sort((a, b) => a.position - b.position) };
        }
        return c;
      });
      this.currentBoard.set({ ...board, columns });
    }
  }

  removeTask(taskId: string, columnId: string) {
    const board = this.currentBoard();
    if (board) {
      const columns = board.columns.map(c => {
        if (c.id === columnId) {
          return { ...c, tasks: c.tasks.filter(t => t.id !== taskId) };
        }
        return c;
      });
      this.currentBoard.set({ ...board, columns });
    }
  }

  moveTask(taskMove: any) {
    this.loadBoard(this.currentBoard()!.id); // simplest approach: reload on move, or implement complex local update
  }

  addActivity(activity: ActivityDto) {
    this.activities.update(acts => [activity, ...acts].slice(0, 50));
  }
}
