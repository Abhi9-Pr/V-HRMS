import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { TokenStorageService } from '../auth/token-storage.service';
import { TenantResolutionService } from '../api/tenant-resolution.service';

const TENANT_HEADER = 'X-Tenant-Id';

/** Authenticated requests get `Authorization: Bearer <token>` (tenant comes from the token's own
 * claim server-side). Pre-auth requests — login, refresh, register, forgot-password, all of
 * which run before any token exists — get `X-Tenant-Id` instead, from whatever tenant the login
 * screen already resolved via TenantResolutionService. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(environment.apiBaseUrl)) {
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
