/*
 * Public API Surface of vespera-shared
 *
 * Everything an app-shell (Vespera.Client, Vespera.Mobile) needs to talk to the API and manage a
 * session, reused as-is rather than forked — see docs/CONTRIBUTING-frontend.md. UI is
 * deliberately out of scope for this library; each shell owns its own screens.
 */

// Generated API client + DI wiring
export * from './lib/api/generated/api-client';
export * from './lib/api/api-config.provider';
export * from './lib/api/tenant-resolution.service';

// Auth
export * from './lib/auth/auth.service';
export * from './lib/auth/auth-session.model';
export * from './lib/auth/jwt.util';
export * from './lib/auth/jwt-claim-types';
export * from './lib/auth/permission.util';
export * from './lib/auth/token-storage.service';

// Authorization
export * from './lib/authorization/permissions';

// HTTP
export * from './lib/http/api-error.model';
export * from './lib/interceptors/auth.interceptor';
export * from './lib/interceptors/correlation-id.interceptor';
export * from './lib/interceptors/error-normalization.interceptor';
export * from './lib/interceptors/loading.interceptor';
export * from './lib/interceptors/refresh.interceptor';

// Cross-cutting services
export * from './lib/services/loading.service';
