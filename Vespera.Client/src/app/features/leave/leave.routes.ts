import { Routes } from '@angular/router';
import { Permissions } from '../../core/authorization/permissions';
import { permissionGuard } from '../../core/guards/permission.guard';

export const LEAVE_ROUTES: Routes = [
  { path: '', redirectTo: 'apply', pathMatch: 'full' },
  {
    path: 'apply',
    loadComponent: () => import('./apply-leave/apply-leave-form.component').then((m) => m.ApplyLeaveFormComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Leave.Request, breadcrumb: 'Apply for leave' },
    title: 'Apply for leave',
  },
  {
    path: 'approvals',
    loadComponent: () => import('./approval-inbox/approval-inbox.component').then((m) => m.ApprovalInboxComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Leave.ReadTeam, breadcrumb: 'Approval inbox' },
    title: 'Approval inbox',
  },
  {
    path: 'delegations',
    loadComponent: () =>
      import('./delegation-settings/delegation-settings.component').then((m) => m.DelegationSettingsComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Leave.ManageDelegation, breadcrumb: 'Delegation settings' },
    title: 'Delegation settings',
  },
  {
    path: 'team-calendar',
    loadComponent: () => import('./team-calendar/team-calendar.component').then((m) => m.TeamCalendarComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Leave.ReadTeam, breadcrumb: 'Team calendar' },
    title: 'Team calendar',
  },
];
