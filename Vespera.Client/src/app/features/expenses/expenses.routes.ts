import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { Permissions } from '../../core/authorization/permissions';

export const EXPENSES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./claim-list/claim-list.component').then((m) => m.ClaimListComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Expenses.Submit, breadcrumb: 'Expenses' },
    title: 'My expense claims',
  },
  {
    path: 'approvals',
    loadComponent: () => import('./approvals/approvals.component').then((m) => m.ApprovalsComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Expenses.Approve, breadcrumb: 'Approvals' },
    title: 'Expense approvals',
  },
  {
    path: 'policies',
    loadComponent: () => import('./policy-admin/policy-admin.component').then((m) => m.PolicyAdminComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Expenses.ManagePolicy, breadcrumb: 'Expense policies' },
    title: 'Expense policies',
  },
  {
    path: 'settlement',
    loadComponent: () => import('./settlement/settlement.component').then((m) => m.SettlementComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Expenses.Settle, breadcrumb: 'Settle claims' },
    title: 'Settle expense claims',
  },
  {
    path: ':id',
    loadComponent: () => import('./claim-detail/claim-detail.component').then((m) => m.ClaimDetailComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Expenses.Submit, breadcrumb: 'Claim' },
    title: 'Expense claim',
  },
];
