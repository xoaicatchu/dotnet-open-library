import { inject, Injectable, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { firstValueFrom, throwError } from 'rxjs';

export interface CurrentUser { id: string; email: string; roles: string[]; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  readonly user = signal<CurrentUser | null>(null);
  private token: string | null = null;
  get requestToken(): string | null { return this.token; }
  async refreshCsrf(): Promise<void> {
    this.token = null;
    this.token = (await firstValueFrom(this.http.get<{ requestToken: string }>('/api/auth/csrf'))).requestToken;
  }
  async initialize(): Promise<void> {
    try { this.user.set(await firstValueFrom(this.http.get<CurrentUser>('/api/auth/me'))); }
    catch (error) {
      this.user.set(null);
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) throw error;
    }
    await this.refreshCsrf();
  }
  async login(email: string, password: string): Promise<void> {
    await this.refreshCsrf();
    await firstValueFrom(this.http.post('/api/auth/login', { email, password }));
    await this.initialize();
  }
  async logout(): Promise<void> {
    await firstValueFrom(this.http.post('/api/auth/logout', {}));
    this.user.set(null);
    await this.refreshCsrf();
  }
}
export const csrfInterceptor: HttpInterceptorFn = (request, next) => {
  const unsafe = !['GET', 'HEAD', 'OPTIONS'].includes(request.method);
  const destination = new URL(request.url, globalThis.location.origin);
  if (!unsafe || destination.origin !== globalThis.location.origin || !destination.pathname.startsWith('/api/')) return next(request);
  const token = inject(AuthService).requestToken;
  if (!token) return throwError(() => new Error('Phiên chống CSRF chưa sẵn sàng. Hãy đăng nhập lại.'));
  return next(request.clone({ setHeaders: { 'X-XSRF-TOKEN': token } }));
};
