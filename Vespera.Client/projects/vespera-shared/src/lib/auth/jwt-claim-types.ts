/**
 * The access token is issued by `System.IdentityModel.Tokens.Jwt` writing raw
 * `System.Security.Claims.ClaimTypes.*` constants as outbound claim keys — .NET only remaps
 * these to short names on the *inbound* (validation) side, so the token actually on the wire
 * carries the long XML/SOAP claim URIs for user id / email / role. `tenant` and `permission` are
 * Vespera's own short custom claim names (see Vespera.Infrastructure.Identity.JwtClaimTypes on
 * the API side). Confirmed against a real token — see docs/CONTRIBUTING-frontend.md.
 */
export const JwtClaimTypes = {
  userId: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
  email: 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
  role: 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
  tenantId: 'tenant',
  permission: 'permission',
} as const;

/** A JWT claim that could be present zero, one, or many times collapses to a scalar, a single
 * value, or an array respectively — this normalizes all three shapes to an array. */
export function claimAsArray(value: unknown): string[] {
  if (value === undefined || value === null) {
    return [];
  }

  return Array.isArray(value) ? value : [value as string];
}
