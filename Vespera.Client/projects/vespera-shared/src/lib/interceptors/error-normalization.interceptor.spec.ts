import { HttpErrorResponse, HttpHeaders, HttpRequest } from '@angular/common/http';
import { firstValueFrom, of, throwError } from 'rxjs';
import { errorNormalizationInterceptor } from './error-normalization.interceptor';
import { ApiError } from '../http/api-error.model';

/** No TestBed needed — this interceptor is a plain function with no `inject()` calls, so it can
 * be invoked directly against a stub `next` that returns an erroring Observable (not one that
 * throws synchronously — `next(req)` must itself be the Observable the interceptor pipes off). */
function invoke(error: unknown) {
  const req = new HttpRequest('GET', '/api/v1/whatever');
  return firstValueFrom(errorNormalizationInterceptor(req, (() => throwError(() => error)) as never)).catch(
    (caught: ApiError) => caught,
  );
}

describe('errorNormalizationInterceptor', () => {
  it('should parse a Blob error body — every generated client requests responseType: "blob" — into code/message from its title/detail', async () => {
    // The real shape a failed request actually arrives in: NSwag's Angular template always
    // requests `responseType: "blob"`, so HttpErrorResponse.error is an unparsed Blob, never
    // already-parsed JSON — this is the exact bug this interceptor exists to guard against.
    const problem = { type: 'about:blank', title: 'auth.totp_required', status: 401, detail: 'A valid authenticator code is required.' };
    const blob = new Blob([JSON.stringify(problem)], { type: 'application/problem+json' });
    const httpError = new HttpErrorResponse({ error: blob, status: 401, statusText: 'Unauthorized' });

    const result = await invoke(httpError);

    expect(result).toEqual<ApiError>({
      status: 401,
      code: 'auth.totp_required',
      message: 'A valid authenticator code is required.',
      correlationId: undefined,
    });
  });

  it('should also handle an already-parsed object error body', async () => {
    const httpError = new HttpErrorResponse({
      error: { title: 'departments.duplicate_code', detail: 'A department with this code already exists.' },
      status: 409,
    });

    const result = await invoke(httpError);

    expect((result as ApiError).code).toBe('departments.duplicate_code');
    expect((result as ApiError).message).toBe('A department with this code already exists.');
  });

  it('should fall back to a generic http.<status> code when the body has no title', async () => {
    const httpError = new HttpErrorResponse({ error: new Blob(['not json']), status: 500 });

    const result = await invoke(httpError);

    expect((result as ApiError).code).toBe('http.500');
    expect((result as ApiError).status).toBe(500);
  });

  it('should carry the X-Correlation-Id response header through when present', async () => {
    const httpError = new HttpErrorResponse({
      error: new Blob([JSON.stringify({ title: 'x', detail: 'y' })]),
      status: 400,
      headers: new HttpHeaders({ 'X-Correlation-Id': 'abc-123' }),
    });

    const result = await invoke(httpError);

    expect((result as ApiError).correlationId).toBe('abc-123');
  });

  it('should pass a non-HttpErrorResponse error through unchanged', async () => {
    const plainError = new Error('boom');

    const result = await invoke(plainError);

    expect(result).toBe(plainError);
  });

  it('should pass a successful response through untouched', async () => {
    const req = new HttpRequest('GET', '/api/v1/whatever');
    const result = await firstValueFrom(errorNormalizationInterceptor(req, () => of('ok') as never));

    expect(result).toBe('ok');
  });
});
