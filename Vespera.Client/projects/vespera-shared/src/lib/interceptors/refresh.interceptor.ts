import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { TokenStorageService } from '../auth/token-storage.service';

// Module-scoped, not per-injector: every concurrent 401 across the whole app must queue behind
// the *same* in-flight refresh call, not one per interceptor instance.
let refreshInProgress = false;
const refreshedToken$ = new BehaviorSubject<string | null>(null);

const EXEMPT_PATHS = ['/api/v1/auth/login', '/api/v1/auth/refresh'];

export const refreshInterceptor: HttpInterceptorFn = (req, next) => {
  if (EXEMPT_PATHS.some((path) => req.url.includes(path))) {
    return next(req);
  }

  const auth = inject(AuthService);
  const tokenStorage = inject(TokenStorageService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || !auth.isAuthenticated()) {
        return throwError(() => error);
      }

      if (!refreshInProgress) {
        refreshInProgress = true;
        refreshedToken$.next(null);

        auth.refresh().subscribe({
          next: () => {
            refreshInProgress = false;
            refreshedToken$.next(tokenStorage.getAccessToken());
          },
          error: () => {
            refreshInProgress = false;
            refreshedToken$.next(null);
          },
        });
      }

      return refreshedToken$.pipe(
        filter((token) => token !== null || !refreshInProgress),
        take(1),
        switchMap((token) => {
          if (!token) {
            return throwError(() => error);
          }

          return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
        }),
      );
    }),
  );
};
