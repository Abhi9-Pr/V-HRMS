import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { APP_INITIALIZER, ApplicationConfig, isDevMode, provideZoneChangeDetection } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideRouter } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { provideTransloco } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { environment } from '../environments/environment';
import { routes } from './app.routes';
import { TranslocoHttpLoader } from './transloco-loader';
import { SessionStorageTokenStorageService } from './core/auth/session-storage-token-storage.service';
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

function restoreSessionOnBootstrap(authService: AuthService) {
  return () => firstValueFrom(authService.tryRestoreSession());
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideNativeDateAdapter(),
    provideVesperaApiClients(environment.apiBaseUrl),
    { provide: TokenStorageService, useClass: SessionStorageTokenStorageService },
    // Order matters — see refresh.interceptor.ts's doc comment: loading must wrap the whole
    // request+retry lifecycle, error normalization must run after refresh has had its chance to
    // retry a 401, and refresh must be innermost so it sees the raw HttpErrorResponse first.
    provideHttpClient(
      withXhr(),
      withInterceptors([
        correlationIdInterceptor,
        authInterceptor,
        loadingInterceptor,
        errorNormalizationInterceptor,
        refreshInterceptor,
      ]),
    ),
    provideStore({}),
    provideEffects([]),
    provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() }),
    provideTransloco({
      config: {
        availableLangs: ['en'],
        defaultLang: 'en',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
    }),
    { provide: APP_INITIALIZER, useFactory: restoreSessionOnBootstrap, deps: [AuthService], multi: true },
  ],
};
