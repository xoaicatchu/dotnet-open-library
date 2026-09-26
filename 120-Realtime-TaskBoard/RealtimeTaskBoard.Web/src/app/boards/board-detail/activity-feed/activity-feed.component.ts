import { Component, inject } from '@angular/core';
import { BoardService } from '../../../services/board.service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-activity-feed',
  standalone: true,
  imports: [DatePipe],
  template: `
    <div class="activity-feed">
      <h3>Activity</h3>
      <div class="activities-list">
        @for (act of boardService.activities(); track act.id) {
          <div class="activity-item">
            <div class="meta">
              <span class="actor">{{ act.actorName }}</span>
              <span class="time">{{ act.timestamp | date:'short' }}</span>
            </div>
            <div class="action">
              {{ act.actionType }} {{ act.entityType }} 
              @if (act.newValue) {
                <span class="value-change">to {{ act.newValue }}</span>
              }
            </div>
          </div>
        } @empty {
          <p class="empty">No activity yet</p>
        }
      </div>
    </div>
  `,
  styles: [`
    .activity-feed { padding: 1rem; }
    .activity-feed h3 { margin-bottom: 1rem; font-size: 1.1rem; }
    .activities-list { display: flex; flex-direction: column; gap: 1rem; }
    .activity-item { font-size: 0.875rem; border-bottom: 1px solid var(--border-color); padding-bottom: 0.5rem; }
    .meta { display: flex; justify-content: space-between; margin-bottom: 0.25rem; color: var(--text-secondary); }
    .actor { font-weight: bold; color: var(--text-color); }
    .time { font-size: 0.75rem; }
    .action { line-height: 1.4; }
    .value-change { font-style: italic; }
    .empty { color: var(--text-secondary); font-size: 0.875rem; text-align: center; }
  `]
})
export class ActivityFeedComponent {
  boardService = inject(BoardService);
}
