import { Injectable } from '@angular/core';
import { SecureStoragePlugin } from 'capacitor-secure-storage-plugin';
import { TokenStorageService } from 'vespera-shared';

const REFRESH_TOKEN_KEY = 'vespera.refreshToken';
const DEVICE_ID_KEY = 'vespera.deviceId';

/**
 * Keychain (iOS) / Keystore-backed EncryptedSharedPreferences (Android) implementation of the
 * `TokenStorageService` port — replaces the interim sessionStorage one from the workspace-scaffold
 * phase. The access token still never leaves memory (same contract everywhere in the app), but the
 * refresh token and device id are now in native secure storage instead of the WebView's scriptable
 * storage, per AGENTS.md's mobile security requirements.
 *
 * The plugin's read/write calls cross the native bridge and are async, but every other consumer of
 * this port (AuthService, the interceptors) calls its methods synchronously, exactly like
 * sessionStorage. Reconciling that: this class keeps an in-memory mirror of the refresh token and
 * device id, hydrated once from secure storage during app bootstrap (see `hydrate()`, awaited by
 * app.config.ts's APP_INITIALIZER before anything else runs) and kept in sync — synchronously in
 * memory, fire-and-forget to the keychain — on every write. Nothing downstream of bootstrap ever
 * observes a stale or un-hydrated value.
 */
@Injectable({ providedIn: 'root' })
export class SecureTokenStorageService implements TokenStorageService {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private deviceId: string | null = null;

  /** Awaited once by app.config.ts's APP_INITIALIZER before any session restore is attempted. */
  async hydrate(): Promise<void> {
    const [refreshToken, deviceId] = await Promise.all([this.readSecure(REFRESH_TOKEN_KEY), this.readSecure(DEVICE_ID_KEY)]);
    this.refreshToken = refreshToken;
    this.deviceId = deviceId;
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  setAccessToken(token: string | null): void {
    this.accessToken = token;
  }

  getRefreshToken(): string | null {
    return this.refreshToken;
  }

  setRefreshToken(token: string | null): void {
    this.refreshToken = token;
    if (token) {
      void SecureStoragePlugin.set({ key: REFRESH_TOKEN_KEY, value: token });
    } else {
      void SecureStoragePlugin.remove({ key: REFRESH_TOKEN_KEY }).catch(() => undefined);
    }
  }

  getOrCreateDeviceId(): string {
    if (!this.deviceId) {
      this.deviceId = crypto.randomUUID();
      void SecureStoragePlugin.set({ key: DEVICE_ID_KEY, value: this.deviceId });
    }

    return this.deviceId;
  }

  clear(): void {
    this.accessToken = null;
    this.refreshToken = null;
    void SecureStoragePlugin.remove({ key: REFRESH_TOKEN_KEY }).catch(() => undefined);
  }

  private async readSecure(key: string): Promise<string | null> {
    try {
      const { value } = await SecureStoragePlugin.get({ key });
      return value;
    } catch {
      // Plugin rejects when the key doesn't exist yet — not a real error, just "nothing stored".
      return null;
    }
  }
}
