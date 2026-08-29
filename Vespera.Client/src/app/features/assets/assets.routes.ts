import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { Permissions } from 'vespera-shared';

export const ASSETS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./asset-list/asset-list.component').then((m) => m.AssetListComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Assets.Read, breadcrumb: 'Assets' },
    title: 'Assets',
  },
  {
    path: 'licenses',
    loadComponent: () => import('./license-list/license-list.component').then((m) => m.LicenseListComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Licenses.Read, breadcrumb: 'Licenses' },
    title: 'Software licenses',
  },
  {
    path: 'licenses/unused-seats-report',
    loadComponent: () =>
      import('./unused-seats-report/unused-seats-report.component').then((m) => m.UnusedSeatsReportComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Licenses.Read, breadcrumb: 'Unused seats' },
    title: 'Unused license seats',
  },
  {
    path: 'recoveries',
    loadComponent: () =>
      import('./recovery-dashboard/recovery-dashboard.component').then((m) => m.RecoveryDashboardComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Assets.Recover, breadcrumb: 'Asset recovery' },
    title: 'Asset recovery',
  },
  {
    path: 'offboarding-checklist/:employeeId',
    loadComponent: () =>
      import('./offboarding-checklist/offboarding-checklist.component').then((m) => m.OffboardingChecklistComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Assets.Recover, breadcrumb: 'Offboarding checklist' },
    title: 'Offboarding checklist',
  },
  {
    path: ':id/handover/:assignmentId',
    loadComponent: () =>
      import('./assignment-handover/assignment-handover.component').then((m) => m.AssignmentHandoverComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Assets.Assign, breadcrumb: 'Handover' },
    title: 'Assignment handover',
  },
  {
    path: ':id',
    loadComponent: () => import('./asset-detail/asset-detail.component').then((m) => m.AssetDetailComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Assets.Read, breadcrumb: 'Asset' },
    title: 'Asset',
  },
];
