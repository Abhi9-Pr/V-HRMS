import { HttpInterceptorFn } from '@angular/common/http';

const CORRELATION_ID_HEADER = 'X-Correlation-Id';

/** Echoes the API's own CorrelationIdMiddleware — every request gets a fresh id the server logs
 * against, so a support request can be traced end to end from a single value. */
export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  const correlationId = crypto.randomUUID();
  return next(req.clone({ setHeaders: { [CORRELATION_ID_HEADER]: correlationId } }));
};
