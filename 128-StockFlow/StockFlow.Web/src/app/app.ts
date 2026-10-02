import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `<header><a routerLink="/" class="brand"><span class="mark">S</span><h1>StockFlow</h1></a><span>Kho cửa hàng</span>@if (auth.user(); as user) { <div class="account"><span>{{ user.email }}</span><button (click)="logout()">Đăng xuất</button></div> }</header><main><router-outlet /></main>`,
  styles: `header{display:flex;align-items:center;gap:24px;padding:16px 24px;background:white;border-bottom:1px solid #dce5e7}.brand{display:flex;align-items:center;gap:10px;text-decoration:none;color:#102e2b}h1{font-size:22px;margin:0}.mark{display:grid;place-items:center;width:34px;height:34px;border-radius:10px;background:#0f766e;color:white;font-weight:800}.account{margin-left:auto;display:flex;align-items:center;gap:16px}button{padding:8px;border:1px solid #dce5e7;border-radius:6px;background:white;color:#102e2b;cursor:pointer}main{padding:24px;max-width:1280px;margin:auto}@media(max-width:600px){header{padding:12px;flex-wrap:wrap;gap:12px}.account{width:100%;margin:0;justify-content:space-between}main{padding:12px}}`
})
export class App {
  readonly auth = inject(AuthService); private readonly router = inject(Router);
  async logout(): Promise<void> { await this.auth.logout(); await this.router.navigateByUrl('/login'); }
}
