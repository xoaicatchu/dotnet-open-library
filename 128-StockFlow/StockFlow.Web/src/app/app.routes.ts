import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth/auth.service';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./auth/login').then(m => m.Login) },
  { path: '', canActivate: [async () => {
    const auth = inject(AuthService); const router = inject(Router);
    try { await auth.initialize(); } catch { return router.createUrlTree(['/login']); }
    return auth.user() ? true : router.createUrlTree(['/login']);
  }], loadComponent: () => import('./dashboard').then(m => m.Dashboard) },
  { path: '**', redirectTo: '' }
];
