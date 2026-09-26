import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.css'
})
export class AppShellComponent {
  // AppShell là layout wrapper chứa header + router-outlet, tương tự khung sườn của trang web.
  isDarkMode = false;

  toggleTheme() {
    this.isDarkMode = !this.isDarkMode;
    // Thay đổi theme bằng cách set attribute data-theme trên thẻ body
    if (this.isDarkMode) {
      document.body.setAttribute('data-theme', 'dark');
    } else {
      document.body.removeAttribute('data-theme');
    }
  }
}
