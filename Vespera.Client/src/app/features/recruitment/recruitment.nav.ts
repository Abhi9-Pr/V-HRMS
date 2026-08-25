import { Permissions } from '../../core/authorization/permissions';
import { NavItem } from '../../core/navigation/nav-item.model';

/** One top-level entry point (the requisition list) — candidate pipeline/detail are reached by
 * drilling into a requisition/card, same as assets.nav.ts didn't give asset-detail/
 * assignment-handover their own nav-tree entries. The public careers page isn't a NavItem at all
 * — it's outside the authenticated shell entirely, registered directly in app.routes.ts. */
export const requisitionsNavItem: NavItem = {
  label: 'Requisitions',
  path: '/recruitment',
  icon: 'work_outline',
  permissions: Permissions.Recruitment.ManageRequisitions,
};
