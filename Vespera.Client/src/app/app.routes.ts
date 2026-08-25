import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./shell/error-pages/forbidden.component').then((m) => m.ForbiddenComponent),
    title: 'Access denied',
  },
  {
    path: '',
    loadComponent: () => import('./shell/shell.component').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'departments', pathMatch: 'full' },
      {
        path: 'departments',
        loadChildren: () => import('./features/departments/departments.routes').then((m) => m.DEPARTMENTS_ROUTES),
      },
      {
        path: 'expenses',
        loadChildren: () => import('./features/expenses/expenses.routes').then((m) => m.EXPENSES_ROUTES),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./shell/error-pages/not-found.component').then((m) => m.NotFoundComponent),
    title: 'Not found',
  },
];
