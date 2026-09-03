# Mobile API integration guide

`Vespera.Mobile` (a Capacitor + Angular shell, added in Phase 13 — see `AGENTS.md`) is currently
scaffolding only, no feature code yet. This is the guide for whoever builds its first real screen:
how to actually talk to the API from a mobile client, end to end. `docs/api-mobile-contract.md`
documents the low-level plumbing (Idempotency-Key replay, ETag, compact-DTO negotiation, delta
sync) in isolation; this doc is how those pieces, plus auth, fit together in a real request.
[ADR 0004](adr/0004-auth-jwt-refresh-step-up-totp.md) covers *why* auth is shaped the way it is.

## The first real mobile route: a worked example

`api/v1/mobile/attendance` (`Vespera.Api/Controllers/V1/Mobile/MobileAttendanceController.cs`) is
the first concrete `api/v1/mobile/*` endpoint in the codebase and the pattern to copy for the next
one:

- `POST api/v1/mobile/attendance/punch` — takes device GPS (`latitude`/`longitude`/`accuracy`),
  a mock-location flag (`isFromMockProvider` — most mobile geolocation APIs expose this; send it
  honestly, the server decides the policy), and a `deviceId`. Send an `Idempotency-Key` header (see
  below) — a punch retried after a flaky connection must not double-record.
- `GET api/v1/mobile/attendance/sync?since=<ISO-8601>&cursor=&pageSize=` — the delta-sync pattern
  from `docs/api-mobile-contract.md` section 4: pass back the previous response's `syncedAt` as the
  next `since`, and its `nextCursor` (if non-null) to page through a large change set. Returns
  `upserts` (changed records) and `tombstonedIds` (deleted since `since`) — reconcile local storage
  against both, not just the upserts.

Both endpoints are self-scoped to the caller's own linked employee record — there's no employee-id
route parameter to get right or wrong; the server resolves it from the authenticated token.

## Auth flow

1. **Resolve the tenant.** `GET api/v1/tenants/by-code/{code}` → `{ tenantId }`. Do this once (or
   cache it) — every subsequent pre-auth call needs the tenant id, not the code.
2. **Log in.** `POST api/v1/auth/login`, header `X-Tenant-Id: <tenantId>`, body
   `{ email, password, deviceId, totpCode }` (`totpCode` optional/omit on the first attempt).
   Success: `{ accessToken, refreshToken, accessTokenExpiresAt }`. On failure, check the
   normalized error's `code` field (see "Error shape" below) — `auth.totp_required` means resubmit
   the same request with a `totpCode`; `auth.totp_enrollment_required` means this account can't
   sign in until TOTP is enrolled first (`POST auth/totp/enroll` then `auth/totp/confirm`) — most
   mobile users won't hit this, it's the Finance-tier step-up from ADR 0004.
3. **Every subsequent request:** `Authorization: Bearer <accessToken>`. No `X-Tenant-Id` needed
   past this point — the tenant is a claim inside the token.
4. **Refresh before (or on) expiry.** `POST api/v1/auth/refresh`, body `{ refreshToken, deviceId }`
   → a new `{ accessToken, refreshToken, accessTokenExpiresAt }` pair — the old refresh token is
   consumed (rotated), so always store the *new* one. Presenting an already-rotated-away refresh
   token revokes every token in its family (reuse detection) — a sign the token was leaked and
   replayed, not just a benign race; if this happens, force a full re-login rather than retrying.
5. **Sign out:** `POST auth/logout` (this device's refresh token) or `auth/logout-all` (every
   device) — access tokens are stateless and can't be revoked early, only refresh tokens can; keep
   access-token lifetime short in whatever client config controls it.

`deviceId` is a client-generated stable identifier (persist it — don't regenerate per launch);
it's part of what scopes a refresh-token family per device rather than per login.

## Error shape

Every non-2xx response is an ASP.NET `ProblemDetails` body: `{ title, detail, status, ... }`. The
Angular client normalizes this to `{ status, code, message, correlationId }` (`code` from `title`,
falling back to `http.<status>` if absent) — build the mobile client's own error handling around
`title`/`code` string matching (`auth.totp_required`, etc.), not HTTP status code alone, since
several distinct failure reasons can share one status code (e.g. 401 covers both "wrong password"
and "valid password, missing TOTP code"). If an error response includes a correlation id header,
log it client-side — it's the fastest way to find the matching server-side log line in Seq when
diagnosing a report from the field (see `docs/observability.md`).

## Idempotency-Key

Generate a fresh UUID once per logical user action (e.g. once when "Punch in" is tapped), and
reuse that *exact* key for any automatic retry of that same tap (a network timeout, a background
sync retry) — never generate a new key per retry, that defeats the point. See
`docs/api-mobile-contract.md` section 1 for the full contract (24-hour cache window, mutating
methods only, 2xx-only caching).

## CORS note

`Vespera:Cors:AllowedOrigins` (`Vespera.Api/appsettings*.json`) is an explicit origin allowlist —
relevant if you're testing against a browser-based dev harness (`ionic serve`, a web preview) but
not to a packaged native app making requests through Capacitor's native HTTP layer, which isn't
subject to browser CORS at all. Don't add a mobile dev-server origin to this list expecting it to
matter for the packaged app; it only matters for browser-based testing.

## Versioning

Every route is `api/v{version:apiVersion}/...`, currently all `1.0`. A breaking change to a mobile-
consumed endpoint should bump the endpoint's own version rather than changing the v1 contract out
from under an already-shipped mobile client build — mobile release cycles (app store review) are
far slower than a web deploy, so v1 stability matters more here than it does for the Angular SPA.
