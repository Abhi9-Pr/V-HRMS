import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResult, StoredSession } from './auth.models';

const SESSION_STORAGE_KEY = 'vespera.session';
const DEVICE_ID_STORAGE_KEY = 'vespera.deviceId';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly session = signal<StoredSession | null>(this.readStoredSession());

  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly accessToken = computed(() => this.session()?.accessToken ?? null);
  readonly tenantId = computed(() => this.session()?.tenantId ?? null);

  constructor(private readonly http: HttpClient) {}

  login(tenantId: string, email: string, password: string): Observable<LoginResult> {
    const request: LoginRequest = { email, password, deviceId: this.deviceId() };

    return this.http
      .post<LoginResult>(`${environment.apiBaseUrl}/auth/login`, request, {
        headers: { 'X-Tenant-Id': tenantId },
      })
      .pipe(
        tap((result) => {
          const stored: StoredSession = { ...result, tenantId };
          localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(stored));
          this.session.set(stored);
        }),
      );
  }

  logout(): void {
    localStorage.removeItem(SESSION_STORAGE_KEY);
    this.session.set(null);
  }

  private deviceId(): string {
    let deviceId = localStorage.getItem(DEVICE_ID_STORAGE_KEY);
    if (!deviceId) {
      deviceId = crypto.randomUUID();
      localStorage.setItem(DEVICE_ID_STORAGE_KEY, deviceId);
    }
    return deviceId;
  }

  private readStoredSession(): StoredSession | null {
    const raw = localStorage.getItem(SESSION_STORAGE_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as StoredSession;
    } catch {
      return null;
    }
  }
}
