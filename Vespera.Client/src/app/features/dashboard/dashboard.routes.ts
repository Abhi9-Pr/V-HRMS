import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { Permissions } from 'vespera-shared';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./dashboard-host/dashboard-host.component').then((m) => m.DashboardHostComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Workspace.ViewDashboard, breadcrumb: 'Dashboard' },
    title: 'Dashboard',
  },
];
