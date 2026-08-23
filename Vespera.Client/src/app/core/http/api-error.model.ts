/** Normalized shape of every failed API call, regardless of whether the server returned RFC 7807
 * ProblemDetails, a bare network failure, or something else — feature code only ever sees this. */
export interface ApiError {
  status: number;
  code: string;
  message: string;
  correlationId?: string;
}
