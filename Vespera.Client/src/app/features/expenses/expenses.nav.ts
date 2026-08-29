import { NavItem } from '../../core/navigation/nav-item.model';
import { Permissions } from 'vespera-shared';

/**
 * Expenses has four screens gated by four different permissions (unlike Departments' single
 * screen/single permission) — a Manager with only Expenses.Approve, say, should still see an
 * "Expense approvals" entry even without Expenses.Submit, so this exports one NavItem per
 * screen rather than one for the whole feature. First multi-screen feature in this codebase;
 * later features with the same shape should follow this, not departmentsNavItem's single-item
 * shape.
 */
export const expensesClaimsNavItem: NavItem = {
  label: 'My Expenses',
  path: '/expenses',
  icon: 'receipt_long',
  permissions: Permissions.Expenses.Submit,
};

export const expensesApprovalsNavItem: NavItem = {
  label: 'Expense Approvals',
  path: '/expenses/approvals',
  icon: 'fact_check',
  permissions: Permissions.Expenses.Approve,
};

export const expensePoliciesNavItem: NavItem = {
  label: 'Expense Policies',
  path: '/expenses/policies',
  icon: 'policy',
  permissions: Permissions.Expenses.ManagePolicy,
};

export const expenseSettlementNavItem: NavItem = {
  label: 'Settle Expenses',
  path: '/expenses/settlement',
  icon: 'payments',
  permissions: Permissions.Expenses.Settle,
};
