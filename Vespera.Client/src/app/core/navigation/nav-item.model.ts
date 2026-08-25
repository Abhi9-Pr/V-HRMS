export interface NavItem {
  label: string;
  path: string;
  icon: string;
  /** Same shape as the matching route's `data['permissions']` — keeping the two declared
   * together in one `<feature>.nav.ts` file next to `<feature>.routes.ts` is what makes this
   * "route-driven": there's no separate hand-maintained nav config to drift out of sync. */
  permissions?: string | string[];
}
