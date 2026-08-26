import { Routes } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';
import { Permissions } from '../../core/authorization/permissions';
import { permissionGuard } from '../../core/guards/permission.guard';
import { payrollFeature } from './store/payroll.reducer';
import * as PayrollEffects from './store/payroll.effects';

/** The NgRx slice is registered here, in this feature's own lazy route providers, per
 * docs/frontend-state.md — never in app.config.ts. It only exists once this route tree is
 * visited. */
export const PAYROLL_ROUTES: Routes = [
  {
    path: '',
    providers: [provideState(payrollFeature), provideEffects(PayrollEffects)],
    children: [
      { path: '', redirectTo: 'runs', pathMatch: 'full' },
      {
        path: 'runs',
        loadComponent: () => import('./payroll-run-wizard/payroll-run-wizard.component').then((m) => m.PayrollRunWizardComponent),
        canActivate: [permissionGuard],
        data: { permissions: Permissions.Payroll.Write, breadcrumb: 'New payroll run' },
        title: 'New payroll run',
      },
      {
        path: 'runs/:id',
        loadComponent: () => import('./payroll-run-wizard/payroll-run-wizard.component').then((m) => m.PayrollRunWizardComponent),
        canActivate: [permissionGuard],
        data: { permissions: Permissions.Payroll.Read, breadcrumb: 'Payroll run' },
        title: 'Payroll run',
      },
      {
        path: 'runs/:id/variance',
        loadComponent: () => import('./variance-review/variance-review.component').then((m) => m.VarianceReviewComponent),
        canActivate: [permissionGuard],
        data: { permissions: Permissions.Payroll.Read, breadcrumb: 'Variance review' },
        title: 'Variance review',
      },
      {
        path: 'runs/:id/payslips/:employeeId',
        loadComponent: () => import('./payslip-viewer/payslip-viewer.component').then((m) => m.PayslipViewerComponent),
        canActivate: [permissionGuard],
        data: { permissions: Permissions.Payroll.Read, breadcrumb: 'Payslip' },
        title: 'Payslip',
      },
      {
        path: 'salary-structures',
        loadComponent: () =>
          import('./salary-structure-editor/salary-structure-editor.component').then((m) => m.SalaryStructureEditorComponent),
        canActivate: [permissionGuard],
        data: { permissions: Permissions.Payroll.Write, breadcrumb: 'Salary structures' },
        title: 'Salary structures',
      },
      {
        path: 'investment-declaration',
        loadComponent: () =>
          import('./investment-declaration/investment-declaration.component').then((m) => m.InvestmentDeclarationComponent),
        canActivate: [permissionGuard],
        data: { permissions: Permissions.Payroll.SelfService, breadcrumb: 'My investment declaration' },
        title: 'My investment declaration',
      },
      {
        path: 'investment-declaration-review',
        loadComponent: () =>
          import('./investment-declaration-review/investment-declaration-review.component').then(
            (m) => m.InvestmentDeclarationReviewComponent,
          ),
        canActivate: [permissionGuard],
        data: { permissions: Permissions.Payroll.Read, breadcrumb: 'Investment declaration review' },
        title: 'Investment declaration review',
      },
    ],
  },
];
