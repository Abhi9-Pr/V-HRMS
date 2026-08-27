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
    // Deliberately outside the authenticated shell — a real careers page has no session at all.
    // See public-jobs.component.ts for how it resolves :tenantCode into the X-Tenant-Id
    // authInterceptor needs, the same TenantResolutionService lookup the login screen already uses.
    path: 'careers/:tenantCode',
    loadComponent: () => import('./features/recruitment/public-jobs/public-jobs.component').then((m) => m.PublicJobsComponent),
    title: 'Open positions',
  },
  {
    path: '',
    loadComponent: () => import('./shell/shell.component').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadChildren: () => import('./features/dashboard/dashboard.routes').then((m) => m.DASHBOARD_ROUTES),
      },
      {
        path: 'departments',
        loadChildren: () => import('./features/departments/departments.routes').then((m) => m.DEPARTMENTS_ROUTES),
      },
      {
        path: 'expenses',
        loadChildren: () => import('./features/expenses/expenses.routes').then((m) => m.EXPENSES_ROUTES),
      },
      {
        path: 'assets',
        loadChildren: () => import('./features/assets/assets.routes').then((m) => m.ASSETS_ROUTES),
      },
      {
        path: 'recruitment',
        loadChildren: () => import('./features/recruitment/recruitment.routes').then((m) => m.RECRUITMENT_ROUTES),
      },
      {
        path: 'helpdesk',
        loadChildren: () => import('./features/helpdesk/helpdesk.routes').then((m) => m.HELPDESK_ROUTES),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./shell/error-pages/not-found.component').then((m) => m.NotFoundComponent),
    title: 'Not found',
  },
];
