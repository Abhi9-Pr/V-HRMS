import { Permissions } from '../../core/authorization/permissions';
import { NavItem } from '../../core/navigation/nav-item.model';

export const dashboardNavItem: NavItem = {
  label: 'Dashboard',
  path: '/dashboard',
  icon: 'dashboard',
  permissions: Permissions.Workspace.ViewDashboard,
};
