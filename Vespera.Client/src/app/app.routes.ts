import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login').then((m) => m.LoginPage) },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./shell/shell').then((m) => m.Shell),
    children: [
      { path: '', redirectTo: 'expenses/claims', pathMatch: 'full' },
      {
        path: 'expenses/claims',
        loadComponent: () => import('./features/expenses/claim-list/claim-list').then((m) => m.ClaimList),
      },
      {
        path: 'expenses/claims/:id',
        loadComponent: () => import('./features/expenses/claim-detail/claim-detail').then((m) => m.ClaimDetail),
      },
      {
        path: 'expenses/approvals',
        loadComponent: () => import('./features/expenses/approvals/approvals').then((m) => m.Approvals),
      },
      {
        path: 'expenses/policies',
        loadComponent: () => import('./features/expenses/policy-admin/policy-admin').then((m) => m.PolicyAdmin),
      },
      {
        path: 'expenses/settlement',
        loadComponent: () => import('./features/expenses/settlement/settlement').then((m) => m.Settlement),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
