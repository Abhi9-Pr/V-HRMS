import { Pipe, PipeTransform, inject } from '@angular/core';
import { TenantSettingsService } from '../../core/services/tenant-settings.service';

/** `{{ amount | vesperaCurrency }}` — honours the tenant's currency (TenantSettingsService) so a
 * value never needs an explicit currency code at the call site. Pass one explicitly only when
 * displaying an amount in a currency other than the tenant's own (e.g. a multi-currency report). */
@Pipe({ name: 'vesperaCurrency', standalone: true, pure: false })
export class CurrencyDisplayPipe implements PipeTransform {
  private readonly tenantSettings = inject(TenantSettingsService);

  transform(value: number | null | undefined, currencyCode?: string): string {
    if (value === null || value === undefined) {
      return '';
    }

    const code = currencyCode ?? this.tenantSettings.currencyCode();
    return new Intl.NumberFormat(undefined, { style: 'currency', currency: code }).format(value);
  }
}
