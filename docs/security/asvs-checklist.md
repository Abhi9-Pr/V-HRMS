# OWASP ASVS walkthrough

A walkthrough of [OWASP ASVS 4.0](https://owasp.org/www-project-application-security-verification-standard/)
against Vespera's actual mechanisms, not a generic checklist — every item cites the file that
implements (or doesn't implement) it. Scoped to Level 1/2 items relevant to a multi-tenant HR/
payroll API; items that don't apply (e.g. native mobile binary protections, GraphQL-specific rules)
are omitted rather than marked N/A for padding. See `docs/data-protection.md` for the DPDP/PII
material this doesn't repeat, and `docs/security/finance-wall-pentest-checklist.md` for the
finance-subsystem-specific checklist.

Status legend: **Met** (verified in code), **Partial** (mechanism exists but has a known gap),
**Gap** (not implemented — a real finding, not a hedge).

## V1 — Architecture

| # | Requirement | Status | Evidence |
|---|---|---|---|
| V1.2 | Authentication architecture documented and consistent | Met | JWT bearer + refresh token rotation, single scheme (`IdentityServiceCollectionExtensions.cs`) |
| V1.4 | Access-control architecture centralized, not ad hoc per-endpoint | Met | `[HasPermission]` custom authorization policy provider (`PermissionAuthorizationPolicyProvider`/`PermissionAuthorizationHandler`) + a structural tenant-isolation query filter (`VesperaDbContext.ApplyGlobalQueryFilters`) — two independent, centrally-defined layers, not per-controller logic |
| V1.5 | Input validation architecture centralized | Met | `ValidationBehavior` in the MediatR pipeline runs every registered `IValidator<TRequest>` before a handler executes — validation isn't optional per-handler |
| V1.9 | Communications architecture — TLS termination point defined | Partial | `SecurityHeadersMiddleware` emits HSTS only when the request already arrived over HTTPS; TLS termination itself is a deployment concern (reverse proxy/load balancer), not yet documented as part of the delivery pipeline — belongs in the zero-downtime/deployment ADR work |

## V2 — Authentication

| # | Requirement | Status | Evidence |
|---|---|---|---|
| V2.1.1 | Minimum password length ≥ 12 | Met | `options.Password.RequiredLength = 12` (`IdentityInfrastructureServiceCollectionExtensions.cs:30`) |
| V2.1.7 | Passwords checked against breach/weak lists | Gap | No `IPasswordValidator` checking against a breached-password list (e.g. HaveIBeenPwned k-anonymity API) — ASP.NET Identity's default validators only check length/character-class composition |
| V2.4.1/V2.4.2 | Passwords stored with a memory-hard or iteration-hardened hash | Met | PBKDF2-HMACSHA256, 210,000 iterations — OWASP's 2023 recommended minimum for this algorithm (`IdentityInfrastructureServiceCollectionExtensions.cs:36`) |
| V2.2.1 | Anti-automation / brute-force protection on login | Partial | `AuthRateLimitPolicyName` caps auth endpoints at 10 requests/minute per client IP (`ObservabilityServiceCollectionExtensions.cs`) — this is IP-based, not account-based, so a distributed (many-IP) credential-stuffing attempt against one account isn't rate-limited by account. No account lockout exists (`Lockout` is explicitly left at ASP.NET Identity's unused default, per the doc comment at `IdentityInfrastructureServiceCollectionExtensions.cs:27`) — a deliberate choice favoring availability over lockout-based DoS risk, but means credential-stuffing detection currently relies entirely on the IP-based limiter |
| V2.5.7 | Multi-factor authentication for high-privilege operations | Met | TOTP required at login for any user holding `Finance.Admin` (`LoginCommandHandler.cs:58-71`) — see the finance-wall checklist for the full mechanism and its limits |
| V2.7 | Session/token binding — tokens can't be replayed after logout | Met | Refresh tokens are hashed at rest and rotated on use (`RefreshTokenRotationTests.cs` exercises this); `LogoutAllDevicesCommandHandler` revokes all of a user's refresh tokens |
| V2.10 | Service-to-service auth doesn't rely on shared long-lived secrets in code | Met | JWT signing key is configuration-bound (`JwtOptions`, validated on start via `JwtOptionsValidator`), never hardcoded; dev-only keys are explicitly named and gitleaks-allowlisted (`.gitleaks.toml`), never used outside Development/Testing |

## V3 — Session Management

| # | Requirement | Status | Evidence |
|---|---|---|---|
| V3.2 | Session tokens are unpredictable, sufficiently random | Met | JWT (signed, standard claims) for access; refresh token is a separate high-entropy value (`ITokenService.GenerateRefreshToken()`), hashed before storage — even a DB read doesn't recover a usable refresh token |
| V3.3 | Logout invalidates the session server-side, not just client-side | Met | Refresh token revocation is server-side (`RefreshToken` entity has a revoked state, not just client-side token deletion) |
| V3.7 | Defense in depth if a token is stolen | Partial | Access tokens are short-lived (`AuthTokenLifetimes.AccessToken`) and stateless — a stolen access token remains valid until natural expiry with no server-side kill switch (only the refresh token can be revoked). This is a standard, accepted JWT tradeoff, not an oversight, but worth naming: a compromised access token can't be force-expired mid-lifetime today |

## V4 — Access Control

| # | Requirement | Status | Evidence |
|---|---|---|---|
| V4.1.1 | Principle of least privilege — deny by default | Met | `[HasPermission(...)]` is required per-endpoint; there's no "authenticated user gets everything" fallback. `FinanceControllerBase` additionally requires `Finance.Admin` specifically — HR/SysAdmin roles are insufficient by design (`FinancePolicyWallTests.cs`) |
| V4.1.3 | Access control enforced server-side, every request, not just at login | Met | `PermissionAuthorizationHandler` evaluates the caller's current permission claims on every request via the JWT's embedded permission codes, not a cached login-time decision outside the token itself |
| V4.2.1 | Sensitive resources have record-level (not just endpoint-level) access control | **Met, per this phase's own IDOR test suite** | The multi-tenant boundary is enforced structurally by a global EF Core query filter on `TenantId` (`VesperaDbContext.ApplyGlobalQueryFilters`), not per-handler logic — meaning cross-tenant access is prevented by construction rather than by remembering to add a check in every handler. This phase adds `CreateSecondTenantAdminClientAsync` and cross-tenant `[Fact]` tests across every CRUD/flow test file in `tests/Vespera.Api.IntegrationTests/` to prove that filter actually holds end-to-end (not just in the DbContext's configuration) on every resource-scoped endpoint, asserting 404 rather than trusting the filter's existence alone |
| V4.2.2 | The system doesn't rely on obscured/unguessable IDs as its only protection | Met | IDs are GUIDs (unguessable) but that's explicitly *not* what's relied on for tenant isolation — the query filter above is the actual boundary, so even a leaked/logged GUID from another tenant doesn't grant access |
| V4.3.1 | Administrative interfaces require additional verification | Met | See V2.5.7 — Finance.Admin (this app's highest-privilege permission tier) requires TOTP, not just the permission claim |

## V5 — Validation, Sanitization, Encoding

| # | Requirement | Status | Evidence |
|---|---|---|---|
| V5.1.1 | Input validation is positive (allow-list), not negative (block-list) | Met | FluentValidation rules on every command are shape/range/format assertions (allow-list), not blocked-pattern checks |
| V5.1.3 | Mass assignment is prevented — the client can't set fields it shouldn't | Met | See this phase's dedicated mass-assignment audit — controllers bind to purpose-built request DTOs, hand-mapped into commands, never `Adapt<Entity>()` or entity-shaped binding. No endpoint accepts a client-supplied `TenantId`, `Id` (on create), or audit field |
| V5.2.3/V5.2.4 | Output encoding prevents XSS/injection in stored data rendered elsewhere | Met | Angular's default template sanitization (Vespera.Client) + the API never returns raw HTML; JSON responses are the only content type on the authenticated surface |
| V5.3.4 | SQL/query injection prevented via parameterization | Met | EF Core parameterizes all generated SQL; the one legitimate raw-SQL/filter-bypass escape hatch (`IgnoreQueryFilters`) is restricted to `ReadRepositoryAdmin` and enforced by `QueryFilterEscapeHatchTests` (see `docs/data-protection.md`) |

## V7 — Error Handling and Logging

| # | Requirement | Status | Evidence |
|---|---|---|---|
| V7.1.1 | Errors don't leak stack traces/internals to the client | Met | `ProblemDetailsExceptionHandler` catches unhandled exceptions and returns RFC 7807 ProblemDetails with a correlation id, not exception details |
| V7.1.2 | Errors are logged with enough context to investigate, without leaking to the response | Met | `RequestLoggingMiddleware` logs method/path/status/duration/tenant/correlation id server-side only — request/response bodies are deliberately never logged (doc comment at `RequestLoggingMiddleware.cs:6-11`) |
| V7.4.1 | Logs don't contain sensitive data (passwords, tokens, full PII) | Met | `AuditLogInterceptor` hashes any `[Pii]`/`IPersonalData` property before it reaches `AuditLog` (see `docs/data-protection.md`); Serilog's own request logging never captures body content |
| — | Structured logs correlate a request through to any async work it triggers | Met (this phase's own work) | `ICorrelationIdProvider` + outbox `CorrelationId` stamping means a request and the background job it caused share one traceable id — see `docs/observability.md` |

## V8 — Data Protection

Covered in full by `docs/data-protection.md` (encryption at rest for PAN/bank account via
`IPiiProtector`, consent tracking, retention policies) — not repeated here.

| # | Requirement | Status | Evidence |
|---|---|---|---|
| V8.3.4 | Sensitive data isn't cached where a wider audience could read it | Met | The one cached endpoint (`PublicJobsController`, output cache) is anonymous/public data by design, and varies by tenant header so one tenant's cache entry never serves another's data (`ObservabilityServiceCollectionExtensions.cs`, `PublicJobsOutputCachePolicyName` doc comment) |

## V9 — Communications

| # | Requirement | Status | Evidence |
|---|---|---|---|
| V9.1.1 | TLS required for all communication carrying sensitive data | Partial | HSTS is emitted conditionally on the request already being HTTPS (`SecurityHeadersMiddleware.cs`) — enforcement (redirecting/rejecting plain HTTP) is a reverse-proxy/deployment concern not yet codified in this repo's own config (compose's `client`/`api` services communicate over plain HTTP inside the Docker network, which is standard practice for a private container network but should be called out explicitly, not left implicit, in the eventual production deployment docs) |
| V9.2.1 | CORS is restrictive, not wildcard | Met | `CorsPolicyName` policy uses an explicit configured allow-list — an empty list means "allow nothing", never a wildcard fallback (doc comment at `ObservabilityServiceCollectionExtensions.cs:45-46`) |

## V13 — API and Web Service

| # | Requirement | Status | Evidence |
|---|---|---|---|
| V13.1.1 | Same access controls enforced on every API version | Met | `[HasPermission]`/tenant-filter mechanisms are version-agnostic — they apply at the DbContext/authorization-handler level, not duplicated per API version |
| V13.2.1 | Requests use a documented, versioned content type | Met | `Asp.Versioning.Mvc` + Swashbuckle-generated OpenAPI; NSwag consumes that same contract for the Angular client, so client and server can't silently drift |
| V13.2.3 | Mutating requests support idempotency where retries are plausible | Met | `Idempotency-Key` header + `IdempotencyBehavior` short-circuits duplicate submissions with a `Conflict` before the handler runs a second time (`IdempotencyReplayTests.cs`) |
| V13.4.1 | Rate limiting protects the API from abuse | Met | Global fixed-window limiter (100 req/min per client) plus tighter policies for auth (10/min) and the public jobs endpoint (30/min) — `ObservabilityServiceCollectionExtensions.cs` |

## Summary of open gaps (not resolved by this phase, tracked for follow-up)

1. **No breached-password check at signup/password-change** (V2.1.7).
2. **No account-level lockout**, only IP-based rate limiting (V2.2.1) — an explicit, documented tradeoff, not an oversight, but worth a second opinion once real traffic patterns exist.
3. **No mid-lifetime access-token revocation** (V3.7) — standard JWT tradeoff; would need a token-denylist or a move to shorter-lived tokens + more frequent refresh if this becomes a real concern.
4. **TLS termination and enforcement isn't yet codified in this repo** (V1.9/V9.1.1) — belongs with the zero-downtime/production-deployment documentation, not this security pass specifically.
