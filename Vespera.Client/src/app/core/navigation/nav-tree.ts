import { departmentsNavItem } from '../../features/departments/departments.nav';
import {
  expenseSettlementNavItem,
  expensePoliciesNavItem,
  expensesApprovalsNavItem,
  expensesClaimsNavItem,
} from '../../features/expenses/expenses.nav';
import { NavItem } from './nav-item.model';

/** Assembled from each feature's own `<feature>.nav.ts` — see nav-item.model.ts for why this
 * can't drift from the matching route's permission metadata. Add one entry per lazy-loaded
 * feature here as it's built. */
export const NAV_TREE: NavItem[] = [
  departmentsNavItem,
  expensesClaimsNavItem,
  expensesApprovalsNavItem,
  expensePoliciesNavItem,
  expenseSettlementNavItem,
];
