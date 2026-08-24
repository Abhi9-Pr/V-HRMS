import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('starts unauthenticated when nothing is stored', () => {
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('sends the tenant id as a pre-auth header and stores the session on success', () => {
    service.login('tenant-1', 'user@vespera.dev', 'secret').subscribe();

    const req = httpMock.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('X-Tenant-Id')).toBe('tenant-1');
    expect(req.request.body.email).toBe('user@vespera.dev');

    req.flush({ accessToken: 'access-1', refreshToken: 'refresh-1', accessTokenExpiresAt: '2026-01-01T00:00:00Z' });

    expect(service.isAuthenticated()).toBeTrue();
    expect(service.accessToken()).toBe('access-1');
    expect(service.tenantId()).toBe('tenant-1');
  });

  it('clears the session on logout', () => {
    service.login('tenant-1', 'user@vespera.dev', 'secret').subscribe();
    httpMock
      .expectOne('/api/v1/auth/login')
      .flush({ accessToken: 'access-1', refreshToken: 'refresh-1', accessTokenExpiresAt: '2026-01-01T00:00:00Z' });

    service.logout();

    expect(service.isAuthenticated()).toBeFalse();
    expect(service.accessToken()).toBeNull();
  });
});
