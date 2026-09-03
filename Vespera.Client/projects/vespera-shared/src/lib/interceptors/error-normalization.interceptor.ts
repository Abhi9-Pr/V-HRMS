import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { ApiError } from '../http/api-error.model';

const CORRELATION_ID_HEADER = 'X-Correlation-Id';

/** Every generated client method (see NSwag's Angular template output) requests
 * `responseType: "blob"`, so a failed request's `HttpErrorResponse.error` arrives as an unparsed
 * `Blob`, never a parsed object — `error.error.title`/`.detail` are `undefined` on a Blob, which
 * silently fell through to a generic "http.<status>" code and Angular's own raw
 * "Http failure response for ..." message on every single failed request in the app, masking the
 * backend's actual RFC 7807 ProblemDetails title/detail entirely. */
function parseErrorBody(error: HttpErrorResponse): Promise<Record<string, unknown>> {
  if (error.error instanceof Blob) {
    return error.error
      .text()
      .then((text) => (text ? (JSON.parse(text) as Record<string, unknown>) : {}))
      .catch(() => ({}));
  }

  return Promise.resolve(typeof error.error === 'object' && error.error !== null ? error.error : {});
}

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

      return from(parseErrorBody(error)).pipe(
        switchMap((problem) => {
          const apiError: ApiError = {
            status: error.status,
            code: (problem['title'] as string | undefined) ?? `http.${error.status}`,
            message: (problem['detail'] as string | undefined) ?? error.message ?? 'An unexpected error occurred.',
            correlationId,
          };

          return throwError(() => apiError);
        }),
      );
    }),
  );
