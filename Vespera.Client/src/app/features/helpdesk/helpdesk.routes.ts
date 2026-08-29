import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { Permissions } from 'vespera-shared';

export const HELPDESK_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./ticket-list/ticket-list.component').then((m) => m.TicketListComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Helpdesk.RaiseTickets, breadcrumb: 'Tickets' },
    title: 'Tickets',
  },
  {
    path: 'sla-dashboard',
    loadComponent: () => import('./sla-dashboard/sla-dashboard.component').then((m) => m.SlaDashboardComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Helpdesk.ViewReports, breadcrumb: 'SLA compliance' },
    title: 'SLA compliance',
  },
  {
    path: 'categories',
    loadComponent: () => import('./category-list/category-list.component').then((m) => m.CategoryListComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Helpdesk.ManageConfiguration, breadcrumb: 'Ticket categories' },
    title: 'Ticket categories',
  },
  {
    path: 'sla-policies',
    loadComponent: () => import('./sla-policy-list/sla-policy-list.component').then((m) => m.SlaPolicyListComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Helpdesk.ManageConfiguration, breadcrumb: 'SLA policies' },
    title: 'SLA policies',
  },
  {
    path: 'holidays',
    loadComponent: () =>
      import('./public-holiday-list/public-holiday-list.component').then((m) => m.PublicHolidayListComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Helpdesk.ManageConfiguration, breadcrumb: 'Public holidays' },
    title: 'Public holidays',
  },
  {
    path: ':id',
    loadComponent: () => import('./ticket-detail/ticket-detail.component').then((m) => m.TicketDetailComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Helpdesk.RaiseTickets, breadcrumb: 'Ticket' },
    title: 'Ticket',
  },
];
