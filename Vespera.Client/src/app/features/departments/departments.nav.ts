import { NavItem } from '../../core/navigation/nav-item.model';
import { Permissions } from 'vespera-shared';

export const departmentsNavItem: NavItem = {
  label: 'Departments',
  path: '/departments',
  icon: 'apartment',
  permissions: Permissions.Departments.Read,
};
