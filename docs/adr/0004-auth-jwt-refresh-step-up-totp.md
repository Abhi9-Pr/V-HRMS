# 0004. Auth: JWT + refresh tokens, step-up TOTP for Finance

**Status:** Accepted

## Context

Vespera needs stateless, horizontally-scalable API authentication (no server-side session store
tying a request to a specific instance) for both the Angular SPA and a future mobile client
(`Vespera.Mobile`), plus a materially higher assurance bar for the handful of actions that move
real money — payroll runs, salary structures — where a stolen password alone being sufficient is an
unacceptable blast radius (see `docs/security/finance-wall-pentest-checklist.md`).

## Decision

Short-lived JWT access tokens (`POST /api/v1/auth/login`), symmetric-key-signed
(`IssuerSigningKey`/`Jwt:SigningKey`), plus a separate refresh token (`POST
/api/v1/auth/refresh`) to obtain a new access token without re-authenticating — `logout`/
`logout-all` revoke refresh tokens rather than trying to invalidate an already-issued stateless
JWT early. On top of that baseline, users holding a Finance-tier role (seeded demo admins
vikram.nair, fatima.khan) must additionally enroll TOTP (`totp/enroll`, `totp/confirm` —
RFC 6238 via `OtpNet`, SHA1/30s-step/6-digit) and supply a valid code on every login: a login
attempt without one gets `needsTotp` back instead of a token pair, then resubmits with the code.
Verification (`OtpService.VerifyTotp`) is stateless — it checks the current window ± 1 step, with
no server-side replay tracking of previously-used codes.

## Alternatives considered

- **Server-side session cookies.** Rejected: ties every request to sticky session state, working
  against the same horizontal-scalability goal the rest of the stateless-API design (and the
  eventual mobile client, which doesn't want cookie-jar semantics) is built around.
- **TOTP (or another second factor) required for every user, not just Finance.** Rejected as the
  default: meaningfully worse day-to-day UX for the large majority of users whose actions carry no
  comparable blast radius, for a marginal security gain outside the specific finance-wall threat
  model this system is built to defend.
- **Replay-protected TOTP** (tracking used codes server-side to reject reuse within a window).
  Not implemented: RFC 6238's ± 1-step verification window already accepts a narrow band for clock
  skew; closing the reuse gap fully would need a persisted "used codes" store purely to prevent an
  attacker who has *already* obtained a valid code within its 30-second window — a narrower
  residual risk than the credential-theft scenario TOTP itself defends against.

## Consequences

- Revoking a compromised access token before its natural expiry isn't possible — only refresh
  tokens are revocable. This bounds the token lifetime choice: too long, and a stolen access token
  stays useful for too long after a `logout-all`; the mitigation is keeping access-token TTL short
  and relying on refresh-token revocation for anything that actually needs to take effect
  immediately.
- The Finance/non-Finance TOTP split means two different login flows the frontend has to handle
  (see the `needsTotp` two-step submit pattern) — a real UX branch, not just a backend detail.
- TOTP's stateless verification means a code is valid for its full window even after first use;
  this is an accepted, bounded residual risk, not an oversight — see Alternatives above.
