import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { APP_INITIALIZER, ApplicationConfig, isDevMode, provideZoneChangeDetection } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';
import { provideRouter } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideStore } from '@ngrx/store';
import { provideStoreDevtools } from '@ngrx/store-devtools';
import { provideTransloco } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { routes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { provideVesperaApiClients } from './core/api/api-config.provider';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { correlationIdInterceptor } from './core/interceptors/correlation-id.interceptor';
import { errorNormalizationInterceptor } from './core/interceptors/error-normalization.interceptor';
import { loadingInterceptor } from './core/interceptors/loading.interceptor';
import { refreshInterceptor } from './core/interceptors/refresh.interceptor';
import { TranslocoHttpLoader } from './transloco-loader';

function restoreSessionOnBootstrap(authService: AuthService) {
  return () => firstValueFrom(authService.tryRestoreSession());
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideNativeDateAdapter(),
    provideVesperaApiClients(),
    // Order matters — see refresh.interceptor.ts's doc comment: loading must wrap the whole
    // request+retry lifecycle, error normalization must run after refresh has had its chance to
    // retry a 401, and refresh must be innermost so it sees the raw HttpErrorResponse first.
    provideHttpClient(
      withInterceptors([correlationIdInterceptor, authInterceptor, loadingInterceptor, errorNormalizationInterceptor, refreshInterceptor]),
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
