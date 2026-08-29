import { Routes } from '@angular/router';
import { permissionGuard } from '../../core/guards/permission.guard';
import { Permissions } from 'vespera-shared';

export const RECRUITMENT_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./requisition-list/requisition-list.component').then((m) => m.RequisitionListComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Recruitment.ManageRequisitions, breadcrumb: 'Requisitions' },
    title: 'Job requisitions',
  },
  {
    path: 'requisitions/:id/pipeline',
    loadComponent: () =>
      import('./candidate-pipeline/candidate-pipeline.component').then((m) => m.CandidatePipelineComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Recruitment.ManageCandidates, breadcrumb: 'Pipeline' },
    title: 'Candidate pipeline',
  },
  {
    path: 'candidates/:id',
    loadComponent: () =>
      import('./candidate-detail/candidate-detail.component').then((m) => m.CandidateDetailComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Recruitment.ManageCandidates, breadcrumb: 'Candidate' },
    title: 'Candidate',
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./requisition-detail/requisition-detail.component').then((m) => m.RequisitionDetailComponent),
    canActivate: [permissionGuard],
    data: { permissions: Permissions.Recruitment.ManageRequisitions, breadcrumb: 'Requisition' },
    title: 'Job requisition',
  },
];
