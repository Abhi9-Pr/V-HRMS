import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, of, tap } from 'rxjs';
import { AuthClient, LoginResult } from '../api/generated/api-client';
import { AuthSession } from './auth-session.model';
import { claimAsArray, JwtClaimTypes } from './jwt-claim-types';
import { decodeJwtPayload } from './jwt.util';
import { TokenStorageService } from './token-storage.service';

interface AccessTokenPayload {
  [JwtClaimTypes.userId]: string;
  [JwtClaimTypes.email]: string;
  [JwtClaimTypes.tenantId]: string;
  [JwtClaimTypes.role]?: string | string[];
  [JwtClaimTypes.permission]?: string | string[];
}

/** The single source of truth for "who is signed in" — every interceptor, guard, and the shell
 * read `session`/`isAuthenticated`/`permissions` from here rather than touching tokens directly. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authClient = inject(AuthClient);
  private readonly tokenStorage = inject(TokenStorageService);

  private readonly sessionSignal = signal<AuthSession | null>(null);

  readonly session = this.sessionSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.sessionSignal() !== null);
  readonly permissions = computed(() => this.sessionSignal()?.permissions ?? []);
  readonly roles = computed(() => this.sessionSignal()?.roles ?? []);

  login(email: string, password: string, totpCode?: string): Observable<void> {
    const deviceId = this.tokenStorage.getOrCreateDeviceId();
    return this.authClient
      .login({ email, password, deviceId, totpCode })
      .pipe(map((result) => this.applyLoginResult(result)));
  }

  /** Called on app bootstrap: a valid refresh token in sessionStorage restores the session
   * silently so a page reload doesn't force a fresh login. */
  tryRestoreSession(): Observable<boolean> {
    const refreshToken = this.tokenStorage.getRefreshToken();
    if (!refreshToken) {
      return of(false);
    }

    return this.refresh().pipe(
      map(() => true),
      catchError(() => {
        this.clearSession();
        return of(false);
      }),
    );
  }

  refresh(): Observable<void> {
    const refreshToken = this.tokenStorage.getRefreshToken();
    const deviceId = this.tokenStorage.getOrCreateDeviceId();
    if (!refreshToken) {
      throw new Error('No refresh token available.');
    }

    return this.authClient.refresh({ refreshToken, deviceId }).pipe(map((result) => this.applyLoginResult(result)));
  }

  logout(): Observable<void> {
    const refreshToken = this.tokenStorage.getRefreshToken();
    const request = refreshToken ? this.authClient.logout({ refreshToken }) : of(undefined);

    return request.pipe(
      tap(() => this.clearSession()),
      catchError(() => {
        this.clearSession();
        return of(undefined);
      }),
    );
  }

  logoutAllDevices(): Observable<void> {
    return this.authClient.logoutAllDevices().pipe(tap(() => this.clearSession()));
  }

  private applyLoginResult(result: LoginResult): void {
    if (!result.accessToken || !result.refreshToken) {
      throw new Error('Login response did not include tokens.');
    }

    this.tokenStorage.setAccessToken(result.accessToken);
    this.tokenStorage.setRefreshToken(result.refreshToken);

    const payload = decodeJwtPayload<AccessTokenPayload>(result.accessToken);
    this.sessionSignal.set({
      userId: payload[JwtClaimTypes.userId],
      email: payload[JwtClaimTypes.email],
      tenantId: payload[JwtClaimTypes.tenantId],
      roles: claimAsArray(payload[JwtClaimTypes.role]),
      permissions: claimAsArray(payload[JwtClaimTypes.permission]),
      accessToken: result.accessToken,
      accessTokenExpiresAt: result.accessTokenExpiresAt ?? new Date(),
    });
  }

  private clearSession(): void {
    this.tokenStorage.clear();
    this.sessionSignal.set(null);
  }
}
