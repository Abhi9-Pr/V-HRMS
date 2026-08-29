import { App } from '@capacitor/app';
import type { AppState } from '@capacitor/app';
import { inject, Injectable, InjectionToken } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from 'vespera-shared';

/** How long the app may sit backgrounded before the next foreground forces a fresh login.
 * Overridable via a provider in app.config.ts; five minutes by default. */
export const AUTO_LOGOUT_TIMEOUT_MS = new InjectionToken<number>('AUTO_LOGOUT_TIMEOUT_MS', {
  providedIn: 'root',
  factory: () => 5 * 60 * 1000,
});

/**
 * Backgrounding the app (home button, app switcher, an incoming call) is the one thing a web
 * session-timeout policy can't see — the WebView keeps running, nothing expires on its own. This
 * service listens for Capacitor's appStateChange, remembers when the app went to the background,
 * and on the next foreground — if more time than the configured timeout has passed — clears the
 * session and sends the user back to login, the same way a 401 does everywhere else in the app.
 *
 * Instantiated once as a side effect of being injected in AppComponent's constructor; nothing
 * else needs to hold a reference to it.
 */
@Injectable({ providedIn: 'root' })
export class AutoLogoutService {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly timeoutMs = inject(AUTO_LOGOUT_TIMEOUT_MS);

  private backgroundedAt: number | null = null;

  // Registered as a field initializer, not constructor-body code, so instantiating this service
  // never needs an explicit constructor — keeping every dependency's resolution on the same
  // field-initializer injection path as the rest of the app (constructor bodies here would still
  // run inside a valid Angular injection context, but there's no reason to mix the two styles).
  private readonly listenerHandle = App.addListener('appStateChange', (state) => this.handleAppStateChange(state));

  /** Exposed for tests — production code only ever reaches this via the appStateChange listener. */
  handleAppStateChange(state: AppState): void {
    if (!state.isActive) {
      this.backgroundedAt = Date.now();
      return;
    }

    const backgroundedAt = this.backgroundedAt;
    this.backgroundedAt = null;

    if (backgroundedAt === null || !this.authService.isAuthenticated()) {
      return;
    }

    if (Date.now() - backgroundedAt >= this.timeoutMs) {
      this.authService.logout().subscribe(() => {
        void this.router.navigateByUrl('/auth/login?reason=session-timeout');
      });
    }
  }
}
