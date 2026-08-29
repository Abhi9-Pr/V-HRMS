import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import {
  AuthService,
  authInterceptor,
  correlationIdInterceptor,
  errorNormalizationInterceptor,
  loadingInterceptor,
  provideVesperaApiClients,
  refreshInterceptor,
  TokenStorageService,
} from 'vespera-shared';
import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { SecureTokenStorageService } from './core/auth/secure-token-storage.service';

/** Secure storage is a native bridge call, so it has to be awaited before session restore can
 * trust what getRefreshToken() returns — see SecureTokenStorageService's doc comment. */
function restoreSessionOnBootstrap(authService: AuthService, tokenStorage: SecureTokenStorageService) {
  return async () => {
    await tokenStorage.hydrate();
    await firstValueFrom(authService.tryRestoreSession());
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideVesperaApiClients(environment.apiBaseUrl),
    { provide: TokenStorageService, useClass: SecureTokenStorageService },
    // Order matters — see refresh.interceptor.ts's doc comment in vespera-shared: loading must
    // wrap the whole request+retry lifecycle, error normalization must run after refresh has had
    // its chance to retry a 401, and refresh must be innermost so it sees the raw
    // HttpErrorResponse first. Mirrors Vespera.Client's app.config.ts exactly.
    provideHttpClient(
      withInterceptors([correlationIdInterceptor, authInterceptor, loadingInterceptor, errorNormalizationInterceptor, refreshInterceptor]),
    ),
    {
      provide: APP_INITIALIZER,
      useFactory: restoreSessionOnBootstrap,
      deps: [AuthService, SecureTokenStorageService],
      multi: true,
    },
  ],
};
