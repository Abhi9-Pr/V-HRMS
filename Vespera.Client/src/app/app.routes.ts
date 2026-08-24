import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login').then((m) => m.LoginPage) },
  {
    path: 'careers/:tenantId',
    loadComponent: () => import('./features/recruitment/public-jobs/public-jobs').then((m) => m.PublicJobs),
  },
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
      {
        path: 'assets',
        loadComponent: () => import('./features/assets/asset-list/asset-list').then((m) => m.AssetList),
      },
      {
        path: 'assets/new',
        loadComponent: () => import('./features/assets/asset-create/asset-create').then((m) => m.AssetCreate),
      },
      {
        path: 'assets/assignments/:assignmentId',
        loadComponent: () =>
          import('./features/assets/assignment-handover/assignment-handover').then((m) => m.AssignmentHandover),
      },
      {
        path: 'assets/licenses',
        loadComponent: () => import('./features/assets/license-list/license-list').then((m) => m.LicenseList),
      },
      {
        path: 'assets/licenses/unused-seats-report',
        loadComponent: () =>
          import('./features/assets/unused-seats-report/unused-seats-report').then((m) => m.UnusedSeatsReport),
      },
      {
        path: 'assets/recoveries',
        loadComponent: () =>
          import('./features/assets/recovery-dashboard/recovery-dashboard').then((m) => m.RecoveryDashboard),
      },
      {
        path: 'assets/offboarding-checklist',
        loadComponent: () =>
          import('./features/assets/offboarding-checklist/offboarding-checklist').then((m) => m.OffboardingChecklist),
      },
      {
        path: 'assets/employee-exit',
        loadComponent: () => import('./features/assets/employee-exit/employee-exit').then((m) => m.EmployeeExit),
      },
      // Registered after 'assets/new' etc. so the static segments above match first.
      {
        path: 'assets/:id',
        loadComponent: () => import('./features/assets/asset-detail/asset-detail').then((m) => m.AssetDetail),
      },
      {
        path: 'recruitment/requisitions',
        loadComponent: () => import('./features/recruitment/requisition-list/requisition-list').then((m) => m.RequisitionList),
      },
      {
        path: 'recruitment/requisitions/new',
        loadComponent: () =>
          import('./features/recruitment/requisition-create/requisition-create').then((m) => m.RequisitionCreate),
      },
      {
        path: 'recruitment/candidates/:id',
        loadComponent: () => import('./features/recruitment/candidate-detail/candidate-detail').then((m) => m.CandidateDetail),
      },
      // Registered after 'recruitment/requisitions/new' so that static segment matches first.
      {
        path: 'recruitment/requisitions/:id/pipeline',
        loadComponent: () =>
          import('./features/recruitment/candidate-pipeline/candidate-pipeline').then((m) => m.CandidatePipeline),
      },
      {
        path: 'recruitment/requisitions/:id',
        loadComponent: () =>
          import('./features/recruitment/requisition-detail/requisition-detail').then((m) => m.RequisitionDetail),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
