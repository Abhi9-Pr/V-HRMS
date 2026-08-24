import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

/// Attaches the bearer token (post-login) and, when available, the pre-auth tenant header.
/// Sending X-Tenant-Id alongside a valid JWT is harmless — HttpTenantContext only falls back to
/// the header when the request is unauthenticated (see login/refresh, which need it before any
/// token exists).
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();
  const tenantId = auth.tenantId();

  if (!token && !tenantId) {
    return next(request);
  }

  let headers = request.headers;
  if (token) {
    headers = headers.set('Authorization', `Bearer ${token}`);
  }
  if (tenantId) {
    headers = headers.set('X-Tenant-Id', tenantId);
  }

  return next(request.clone({ headers }));
};
