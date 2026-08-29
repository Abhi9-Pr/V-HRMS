import { Injectable } from '@angular/core';
import { TokenStorageService } from 'vespera-shared';

const REFRESH_TOKEN_KEY = 'vespera.refreshToken';
const DEVICE_ID_KEY = 'vespera.deviceId';

/**
 * Decision (docs/CONTRIBUTING-frontend.md): the access token lives only in memory (a private
 * field here, never persisted) — a page reload always re-authenticates via the refresh token.
 * The refresh token would ideally be an httpOnly cookie the browser can't script-read, but
 * `POST /auth/refresh` takes it in the JSON body and the API sets no cookies at all, so true
 * httpOnly storage isn't possible without a backend change out of this phase's scope. sessionStorage
 * (tab-scoped, gone on tab close, not shared cross-tab like localStorage) is the pragmatic
 * middle ground. Registered as the `TokenStorageService` port's implementation in app.config.ts.
 */
@Injectable({ providedIn: 'root' })
export class SessionStorageTokenStorageService implements TokenStorageService {
  private accessToken: string | null = null;

  getAccessToken(): string | null {
    return this.accessToken;
  }

  setAccessToken(token: string | null): void {
    this.accessToken = token;
  }

  getRefreshToken(): string | null {
    return sessionStorage.getItem(REFRESH_TOKEN_KEY);
  }

  setRefreshToken(token: string | null): void {
    if (token) {
      sessionStorage.setItem(REFRESH_TOKEN_KEY, token);
    } else {
      sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    }
  }

  getOrCreateDeviceId(): string {
    let deviceId = sessionStorage.getItem(DEVICE_ID_KEY);
    if (!deviceId) {
      deviceId = crypto.randomUUID();
      sessionStorage.setItem(DEVICE_ID_KEY, deviceId);
    }

    return deviceId;
  }

  clear(): void {
    this.accessToken = null;
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  }
}
