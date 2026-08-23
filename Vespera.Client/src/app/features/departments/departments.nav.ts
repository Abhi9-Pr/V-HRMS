import { Permissions } from '../../core/authorization/permissions';
import { NavItem } from '../../core/navigation/nav-item.model';

export const departmentsNavItem: NavItem = {
  label: 'Departments',
  path: '/departments',
  icon: 'apartment',
  permissions: Permissions.Departments.Read,
};
