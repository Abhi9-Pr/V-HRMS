/**
 * Port every app-shell provides a concrete implementation for. The access token is always
 * expected to live in memory only (never persisted) — a reload/relaunch re-authenticates via the
 * refresh token. Where and how the refresh token and device id persist is a per-shell decision:
 * Vespera.Client uses sessionStorage (see docs/CONTRIBUTING-frontend.md), Vespera.Mobile uses
 * platform secure storage (Keychain/Keystore). Everything else in this library reads/writes
 * through this one abstraction, so swapping the concrete storage touches only the app-shell's own
 * implementation, never shared code.
 */
export abstract class TokenStorageService {
  abstract getAccessToken(): string | null;
  abstract setAccessToken(token: string | null): void;
  abstract getRefreshToken(): string | null;
  abstract setRefreshToken(token: string | null): void;
  abstract getOrCreateDeviceId(): string;
  abstract clear(): void;
}
