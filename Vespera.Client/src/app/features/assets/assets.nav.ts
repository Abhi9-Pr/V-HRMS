import { Permissions } from '../../core/authorization/permissions';
import { NavItem } from '../../core/navigation/nav-item.model';

/** Assets has four top-level screens gated by different permissions, same multi-item shape
 * expensesNavItem* established — a caller with only Assets.Recover, say, should still see the
 * recovery dashboard without also having Assets.Read. Sub-screens reached by drilling into a row
 * (asset detail, assignment handover, offboarding checklist) don't get their own nav item, same
 * as claim-detail didn't in expenses.nav.ts. */
export const assetsListNavItem: NavItem = {
  label: 'Assets',
  path: '/assets',
  icon: 'inventory_2',
  permissions: Permissions.Assets.Read,
};

export const licensesNavItem: NavItem = {
  label: 'Licenses',
  path: '/assets/licenses',
  icon: 'vpn_key',
  permissions: Permissions.Licenses.Read,
};

export const unusedSeatsReportNavItem: NavItem = {
  label: 'Unused Seats',
  path: '/assets/licenses/unused-seats-report',
  icon: 'assessment',
  permissions: Permissions.Licenses.Read,
};

export const assetRecoveryDashboardNavItem: NavItem = {
  label: 'Asset Recovery',
  path: '/assets/recoveries',
  icon: 'assignment_return',
  permissions: Permissions.Assets.Recover,
};
