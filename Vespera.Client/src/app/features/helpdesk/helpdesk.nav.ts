import { NavItem } from '../../core/navigation/nav-item.model';
import { Permissions } from 'vespera-shared';

/** Five top-level screens gated by different permissions, same multi-item shape assets.nav.ts
 * established — a caller with only Helpdesk.ViewReports, say, should still see the SLA dashboard
 * without also having RaiseTickets. Ticket detail is reached by drilling into a row and doesn't
 * get its own nav item, same as asset-detail/claim-detail didn't. */
export const ticketsNavItem: NavItem = {
  label: 'Tickets',
  path: '/helpdesk',
  icon: 'confirmation_number',
  permissions: Permissions.Helpdesk.RaiseTickets,
};

export const slaDashboardNavItem: NavItem = {
  label: 'SLA Compliance',
  path: '/helpdesk/sla-dashboard',
  icon: 'assessment',
  permissions: Permissions.Helpdesk.ViewReports,
};

export const ticketCategoriesNavItem: NavItem = {
  label: 'Ticket Categories',
  path: '/helpdesk/categories',
  icon: 'category',
  permissions: Permissions.Helpdesk.ManageConfiguration,
};

export const slaPoliciesNavItem: NavItem = {
  label: 'SLA Policies',
  path: '/helpdesk/sla-policies',
  icon: 'rule',
  permissions: Permissions.Helpdesk.ManageConfiguration,
};

export const publicHolidaysNavItem: NavItem = {
  label: 'Public Holidays',
  path: '/helpdesk/holidays',
  icon: 'event',
  permissions: Permissions.Helpdesk.ManageConfiguration,
};
