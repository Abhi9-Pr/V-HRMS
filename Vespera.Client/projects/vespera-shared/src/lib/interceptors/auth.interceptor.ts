import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { TokenStorageService } from '../auth/token-storage.service';
import { TenantResolutionService } from '../api/tenant-resolution.service';
import { API_BASE_URL } from '../api/generated/api-client';

const TENANT_HEADER = 'X-Tenant-Id';

/** Authenticated requests get `Authorization: Bearer <token>` (tenant comes from the token's own
 * claim server-side). Pre-auth requests — login, refresh, register, forgot-password, all of
 * which run before any token exists — get `X-Tenant-Id` instead, from whatever tenant the login
 * screen already resolved via TenantResolutionService. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // `apiBaseUrl` is legitimately '' for a same-origin production deployment (see
  // environment.prod.ts) — `''.startsWith('')` is true for every request, which is correct and
  // intended. Only skip this interceptor when the DI token itself was never provided at all
  // (`== null` catches both null and undefined, but not ''); a `!apiBaseUrl` falsy check would
  // wrongly treat '' the same as "no token provided" and silently skip every request, which is
  // exactly what happened here before this fix — no Authorization or X-Tenant-Id header was ever
  // attached in any same-origin deployment.
  const apiBaseUrl = inject(API_BASE_URL, { optional: true });
  if (apiBaseUrl == null || !req.url.startsWith(apiBaseUrl)) {
    return next(req);
  }

  const auth = inject(AuthService);
  const tokenStorage = inject(TokenStorageService);
  const tenantResolution = inject(TenantResolutionService);

  const accessToken = tokenStorage.getAccessToken();
  if (auth.isAuthenticated() && accessToken) {
    return next(req.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }));
  }

  const tenantId = tenantResolution.getCachedTenantId();
  if (tenantId) {
    return next(req.clone({ setHeaders: { [TENANT_HEADER]: tenantId } }));
  }

  return next(req);
};
