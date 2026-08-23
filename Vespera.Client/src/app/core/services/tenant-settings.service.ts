import { Injectable, signal } from '@angular/core';

/**
 * Seam for a future tenant-settings feature: `Tenant` has no `Currency` field on the backend yet
 * (this phase is shell-only, no HR/settings feature), so `currencyCode` defaults to "USD" until
 * something real populates it. vespera-currency reads this rather than a literal so that day-one
 * wiring is a one-line change here, not a hunt through every currency display in the app.
 */
@Injectable({ providedIn: 'root' })
export class TenantSettingsService {
  private readonly currencyCodeSignal = signal('USD');

  readonly currencyCode = this.currencyCodeSignal.asReadonly();

  setCurrencyCode(code: string): void {
    this.currencyCodeSignal.set(code);
  }
}
