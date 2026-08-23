import { Permissions } from '../../core/authorization/permissions';
import { NavItem } from '../../core/navigation/nav-item.model';

export const payrollRunsNavItem: NavItem = {
  label: 'Payroll Runs',
  path: '/payroll/runs',
  icon: 'payments',
  permissions: Permissions.Payroll.Write,
};

export const salaryStructuresNavItem: NavItem = {
  label: 'Salary Structures',
  path: '/payroll/salary-structures',
  icon: 'account_tree',
  permissions: Permissions.Payroll.Write,
};

export const myInvestmentDeclarationNavItem: NavItem = {
  label: 'My Investment Declaration',
  path: '/payroll/investment-declaration',
  icon: 'receipt_long',
  permissions: Permissions.Payroll.SelfService,
};

export const investmentDeclarationReviewNavItem: NavItem = {
  label: 'Investment Declaration Review',
  path: '/payroll/investment-declaration-review',
  icon: 'fact_check',
  permissions: Permissions.Payroll.Read,
};
