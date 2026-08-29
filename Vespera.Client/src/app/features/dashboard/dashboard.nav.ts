import { NavItem } from '../../core/navigation/nav-item.model';
import { Permissions } from 'vespera-shared';

export const dashboardNavItem: NavItem = {
  label: 'Dashboard',
  path: '/dashboard',
  icon: 'dashboard',
  permissions: Permissions.Workspace.ViewDashboard,
};
