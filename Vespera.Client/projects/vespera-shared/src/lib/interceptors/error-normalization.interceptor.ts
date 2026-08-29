import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { ApiError } from '../http/api-error.model';

const CORRELATION_ID_HEADER = 'X-Correlation-Id';

/** Converts every failed request — RFC 7807 ProblemDetails from the API, or a bare network
 * error — into one ApiError shape, so feature code never branches on response format. Runs
 * outermost of the HTTP-error-producing interceptors so it sees the final error either way. */
export const errorNormalizationInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse)) {
        return throwError(() => error);
      }

      const correlationId = error.headers?.get(CORRELATION_ID_HEADER) ?? undefined;
      const problem = typeof error.error === 'object' && error.error !== null ? error.error : {};

      const apiError: ApiError = {
        status: error.status,
        code: problem.title ?? `http.${error.status}`,
        message: problem.detail ?? error.message ?? 'An unexpected error occurred.',
        correlationId,
      };

      return throwError(() => apiError);
    }),
  );
