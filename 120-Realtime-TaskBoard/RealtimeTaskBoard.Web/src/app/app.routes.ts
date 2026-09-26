import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: '/boards', pathMatch: 'full' },
  {
    path: 'boards',
    loadComponent: () => import('./presentation/board-list/board-list.component').then(m => m.BoardListComponent)
  },
  {
    path: 'boards/:id',
    loadComponent: () => import('./presentation/board-detail/board-detail.component').then(m => m.BoardDetailComponent)
  }
];
