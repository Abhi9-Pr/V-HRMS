import { Pipe, PipeTransform, inject } from '@angular/core';
import { UserPreferencesService } from '../../core/services/user-preferences.service';

/** `{{ instant | vesperaDate }}` — every persisted timestamp is UTC (AGENTS.md); this renders it
 * in the signed-in user's own timezone (UserPreferencesService), never the server's or the
 * browser's timezone implicitly. */
@Pipe({ name: 'vesperaDate', standalone: true, pure: false })
export class TimezoneDatePipe implements PipeTransform {
  private readonly userPreferences = inject(UserPreferencesService);

  transform(value: Date | string | null | undefined, options?: Intl.DateTimeFormatOptions): string {
    if (!value) {
      return '';
    }

    const date = typeof value === 'string' ? new Date(value) : value;
    const formatOptions: Intl.DateTimeFormatOptions = options ?? {
      dateStyle: 'medium',
      timeStyle: 'short',
    };

    return new Intl.DateTimeFormat(undefined, { ...formatOptions, timeZone: this.userPreferences.timezone() }).format(date);
  }
}
