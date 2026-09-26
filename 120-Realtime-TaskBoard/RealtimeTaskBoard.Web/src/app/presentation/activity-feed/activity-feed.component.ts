import { Component, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { BoardStateService } from '../../application/state/board-state.service';

@Component({
  selector: 'app-activity-feed',
  standalone: true,
  imports: [CommonModule],
  providers: [DatePipe],
  templateUrl: './activity-feed.component.html',
  styleUrl: './activity-feed.component.css'
})
export class ActivityFeedComponent {
  // Sidebar hiển thị lịch sử hoạt động real-time.
  boardState = inject(BoardStateService);
}
