import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService, hasPermission } from 'vespera-shared';

/** Reads `route.data['permissions']` (a string or string[]) — a route with no `permissions` key
 * is allowed through untouched. Pair with authGuard (permissionGuard doesn't itself check
 * authentication). */
export const permissionGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const required = route.data['permissions'] as string | string[] | undefined;
  if (hasPermission(auth.permissions(), required)) {
    return true;
  }

  return router.createUrlTree(['/forbidden']);
};
