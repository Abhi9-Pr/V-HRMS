import { Injectable, signal } from '@angular/core';

const TIMEZONE_KEY = 'vespera.timezone';

/** Backs vespera-date (timezone-aware date pipe). Defaults to the browser's own timezone —
 * matches the backend's own stance that display timezone is a per-user setting (AGENTS.md),
 * not a fixed server default. */
@Injectable({ providedIn: 'root' })
export class UserPreferencesService {
  private readonly timezoneSignal = signal(localStorage.getItem(TIMEZONE_KEY) ?? Intl.DateTimeFormat().resolvedOptions().timeZone);

  readonly timezone = this.timezoneSignal.asReadonly();

  setTimezone(timezone: string): void {
    this.timezoneSignal.set(timezone);
    localStorage.setItem(TIMEZONE_KEY, timezone);
  }
}
