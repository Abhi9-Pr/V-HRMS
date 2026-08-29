import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { Permissions } from 'vespera-shared';

export const DEPARTMENTS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./department-list/department-list.component').then((m) => m.DepartmentListComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Departments.Read, breadcrumb: 'Departments' },
    title: 'Departments',
  },
];
