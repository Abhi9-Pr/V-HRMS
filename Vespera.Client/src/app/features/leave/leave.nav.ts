import { Permissions } from '../../core/authorization/permissions';
import { NavItem } from '../../core/navigation/nav-item.model';

export const applyLeaveNavItem: NavItem = {
  label: 'Apply for Leave',
  path: '/leave/apply',
  icon: 'event_available',
  permissions: Permissions.Leave.Request,
};

export const leaveApprovalsNavItem: NavItem = {
  label: 'Leave Approvals',
  path: '/leave/approvals',
  icon: 'fact_check',
  permissions: Permissions.Leave.ReadTeam,
};

export const leaveDelegationsNavItem: NavItem = {
  label: 'Leave Delegation',
  path: '/leave/delegations',
  icon: 'person_pin_circle',
  permissions: Permissions.Leave.ManageDelegation,
};

export const teamCalendarNavItem: NavItem = {
  label: 'Team Calendar',
  path: '/leave/team-calendar',
  icon: 'calendar_month',
  permissions: Permissions.Leave.ReadTeam,
};
