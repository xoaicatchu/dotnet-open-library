import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <header class="app-header">
      <div class="logo">
        <a routerLink="/boards">Kanban Board</a>
      </div>
      <div class="theme-toggle">
        <button class="btn" (click)="toggleTheme()">Toggle Theme</button>
      </div>
    </header>
    <main class="app-content">
      <router-outlet></router-outlet>
    </main>
  `,
  styles: [`
    .app-header {
      background-color: var(--primary-color);
      color: white;
      padding: 0 1rem;
      height: 48px;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .logo a {
      color: white;
      text-decoration: none;
      font-weight: bold;
      font-size: 1.25rem;
    }
    .app-content {
      height: calc(100vh - 48px);
      overflow: hidden;
    }
    .theme-toggle button {
      background: transparent;
      color: white;
      border: 1px solid rgba(255,255,255,0.5);
    }
  `]
})
export class AppComponent {
  toggleTheme() {
    const isDark = document.body.getAttribute('data-theme') === 'dark';
    document.body.setAttribute('data-theme', isDark ? 'light' : 'dark');
  }
}
