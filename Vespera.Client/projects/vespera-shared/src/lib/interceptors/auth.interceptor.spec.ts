import { HttpRequest } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { API_BASE_URL } from '../api/generated/api-client';
import { AuthService } from '../auth/auth.service';
import { TokenStorageService } from '../auth/token-storage.service';
import { TenantResolutionService } from '../api/tenant-resolution.service';

function configure(opts: { apiBaseUrl: string | null; isAuthenticated: boolean; accessToken: string | null; tenantId: string | null }) {
  const providers = [
    { provide: AuthService, useValue: { isAuthenticated: () => opts.isAuthenticated } },
    { provide: TokenStorageService, useValue: { getAccessToken: () => opts.accessToken } },
    { provide: TenantResolutionService, useValue: { getCachedTenantId: () => opts.tenantId } },
  ];
  if (opts.apiBaseUrl !== null) {
    providers.push({ provide: API_BASE_URL, useValue: opts.apiBaseUrl });
  }
  TestBed.configureTestingModule({ providers });
}

function invoke(url: string) {
  const req = new HttpRequest('GET', url);
  let seen: HttpRequest<unknown> | undefined;
  const next = (r: HttpRequest<unknown>) => {
    seen = r;
    return of('ok');
  };
  return TestBed.runInInjectionContext(() => {
    authInterceptor(req, next as never).subscribe();
    return seen!;
  });
}

describe('authInterceptor', () => {
  it('attaches X-Tenant-Id for a same-origin deployment (apiBaseUrl === "") — the exact case that regressed', () => {
    // A same-origin production deployment (nginx reverse-proxying /api/ to the api container —
    // see environment.prod.ts) legitimately provides API_BASE_URL as '', not a real absolute URL.
    // A `!apiBaseUrl` falsy check treats '' the same as "no base URL configured at all" and skips
    // this interceptor entirely — silently dropping every Authorization/X-Tenant-Id header in any
    // same-origin deployment. This test pins the fix: '' must still match every request.
    configure({ apiBaseUrl: '', isAuthenticated: false, accessToken: null, tenantId: 'tenant-123' });

    const sent = invoke('/api/v1/auth/login');

    expect(sent.headers.get('X-Tenant-Id')).toBe('tenant-123');
  });

  it('attaches Authorization for a same-origin deployment when already authenticated', () => {
    configure({ apiBaseUrl: '', isAuthenticated: true, accessToken: 'jwt-abc', tenantId: null });

    const sent = invoke('/api/v1/employees/me');

    expect(sent.headers.get('Authorization')).toBe('Bearer jwt-abc');
  });

  it('attaches X-Tenant-Id when apiBaseUrl is a real absolute URL and the request targets it (dev-server case)', () => {
    configure({ apiBaseUrl: 'https://localhost:7095', isAuthenticated: false, accessToken: null, tenantId: 'tenant-456' });

    const sent = invoke('https://localhost:7095/api/v1/auth/login');

    expect(sent.headers.get('X-Tenant-Id')).toBe('tenant-456');
  });

  it('leaves the request untouched when the API_BASE_URL token was never provided at all', () => {
    configure({ apiBaseUrl: null, isAuthenticated: false, accessToken: null, tenantId: 'tenant-123' });

    const sent = invoke('/api/v1/auth/login');

    expect(sent.headers.has('X-Tenant-Id')).toBe(false);
    expect(sent.headers.has('Authorization')).toBe(false);
  });

  it('leaves a request to a different origin untouched when apiBaseUrl is a real absolute URL', () => {
    configure({ apiBaseUrl: 'https://localhost:7095', isAuthenticated: true, accessToken: 'jwt-abc', tenantId: null });

    const sent = invoke('https://some-other-service.example.com/thing');

    expect(sent.headers.has('Authorization')).toBe(false);
  });

  it('attaches neither header when unauthenticated with no cached tenant', () => {
    configure({ apiBaseUrl: '', isAuthenticated: false, accessToken: null, tenantId: null });

    const sent = invoke('/api/v1/tenants/by-code/DEMO');

    expect(sent.headers.has('X-Tenant-Id')).toBe(false);
    expect(sent.headers.has('Authorization')).toBe(false);
  });
});
