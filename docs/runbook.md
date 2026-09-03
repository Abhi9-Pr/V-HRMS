# Runbook

Operational reference for a running Vespera deployment — health checks, logs/traces, rollback,
migrations, and known-pattern incidents. See `docs/deployment.md` for how the CI pipeline
publishes and promotes images in the first place; this doc is what to do once something is
already running and needs attention.

## Health checks

- `GET /health/live` — process liveness only (always 200 if the process is up). Used by the
  Dockerfile `HEALTHCHECK` and CI's "wait for the container" polling loops.
- `GET /health/ready` — checks `database`, `outbox`, and `storage` health checks
  (`ObservabilityServiceCollectionExtensions`). A 200 here is the real "safe to send traffic"
  signal; `tools/smoke-test.sh`/`.ps1` polls this before running its own checks.

If `/health/ready` is failing but `/health/live` is fine, the process is up but one of its
dependencies isn't — check which check failed in the response body, then jump to the matching
section below (database, storage).

## Logs and traces

Structured logs and distributed traces/metrics both land in Seq (`http://localhost:8082` via
docker-compose) but travel there via two independent pipelines — see
[ADR 0006](adr/0006-observability-serilog-otel-split.md) and `docs/observability.md` for why.
Search Seq by `CorrelationId` (every response carries one in its headers, and the Angular client
surfaces it on every normalized error — see `docs/mobile-integration-guide.md`'s error-shape
section) to pull every log line for one specific failed request.

## Rolling back a bad deploy

Nothing in this repo deploys to a live host yet (see
[ADR 0005](adr/0005-deployment-target-ghcr-docker-compose.md)) — "rollback" here means re-pointing
the `production`/`latest` GHCR tags at a previous known-good commit's image, which is what any
future host would actually pull.

1. Find the last good commit's short SHA (`git log --oneline master`).
2. Re-run the promotion step by hand (the same operation `deploy-production` runs, just against an
   older tag):
   ```bash
   docker buildx imagetools create \
     --tag ghcr.io/<owner>/vespera-api:production \
     --tag ghcr.io/<owner>/vespera-api:latest \
     ghcr.io/<owner>/vespera-api:sha-<good-short-sha>
   # repeat for vespera-client
   ```
3. If a real host is pulling `:production` on a schedule or via webhook, it picks this up on its
   next pull; if it's a long-running container, restart it to force an immediate pull.

This is a manifest re-tag, not a rebuild — it's fast and doesn't re-run CI.

## Database

**Local dev container** (`vespera-db-dev`, the Phase 3 auto-provisioner — see
`docs/docker-compose.md` for how this differs from the docker-compose stack's own `db` service):
`tools/vespera-db.sh {up|down|reset|status|logs}` (`.ps1` on Windows). `reset` drops and recreates
the container's volume — destructive, local-dev-only, never point this at anything containing data
you need.

**docker-compose stack's `db` service:** standard Postgres in a named volume (`pgdata`). Backup:
```bash
docker compose exec db pg_dump -U vespera vespera > backup.sql
```
Restore into a fresh volume:
```bash
docker compose exec -T db psql -U vespera vespera < backup.sql
```

**Migrations.** Both the dev auto-provisioner and the docker-compose stack's `Development`-
environment API auto-migrate on startup — fine for those. A `Production`-environment deploy does
**not** auto-migrate (`DatabaseServiceCollectionExtensions.cs`); apply pending migrations
explicitly before the new API image goes live:
```bash
./tools/publish-migrations-bundle.sh            # builds ./artifacts/efbundle
./artifacts/efbundle --connection "Host=...;Database=...;Username=...;Password=..."
```
Run this *before* restarting the API on the new image — a new image expecting a column that
doesn't exist yet will crash-loop or 500 on the first request that touches it.

## Auth rate limiting

`AuthRateLimitPolicyName`: 10 requests/minute, applied to every `AuthController` action
(login/refresh/register/password-reset/TOTP), partitioned by caller. If a user (or, more likely
during development, a script iterating logins) reports repeated 429s on login: this is the rate
limiter working as designed, not a bug — the fix is waiting out the window (a minute), not
loosening the limit. There's also a global 100/minute limiter and a 30/minute `PublicApi` policy
for unauthenticated public-jobs-style endpoints (`ObservabilityServiceCollectionExtensions.cs` has
the exact numbers if they've since changed).

## JWT signing key rotation

The signing key (`Jwt:SigningKey` in configuration — a secret, never committed; see `AGENTS.md`'s
"never commit secrets" rule) is symmetric — rotating it immediately invalidates every access token
issued under the old key (not refresh tokens, which are validated against the database, not the
JWT signature). Plan a rotation as a deliberate "everyone gets logged out of their current access
token, refresh flow picks up on their next silent-refresh attempt" event, not a zero-impact change.

## Known-pattern incidents

**Dashboard widgets randomly failing, different subset each request.** This exact symptom was a
real bug found during this engagement's testing phase: `GetDashboardQueryHandler` ran every
widget's fetch concurrently (`Task.WhenAll`) against one shared, request-scoped `DbContext`,
throwing `InvalidOperationException: A second operation was started on this context instance...`
— silently caught and swallowed with no logging at all, so it was invisible until logging was
added specifically to diagnose it. Fixed via a per-widget `IServiceScopeFactory.CreateScope()`
(each widget fetch gets its own DbContext) plus proper `LoggerMessage.Define`-based logging on
failure. If this symptom resurfaces anywhere else in the codebase, it's almost certainly the same
root cause: two concurrent operations sharing one scoped `DbContext` — check for a `Task.WhenAll`
(or similar fan-out) over MediatR `Send` calls that all resolve the ambient request scope, and fix
it the same way.

**A payroll dry-run or run is slow.** `.github/workflows/ci.yml`'s `payroll-performance-budget`
job guards against a regression here (currently a 5-second budget on a 50-employee cohort, one
uncontended sample) — if it's failing in CI, `docs/performance.md` documents the N+1 patterns
already found and fixed in this area and is the first place to look before assuming a new one.

**Payslip generation 500s with a `DirectoryNotFoundException`.** Another real, fixed bug:
`LocalFileStorage.UploadAsync` didn't create parent directories for storage keys containing a
subdirectory component (e.g. `payslips/2026-09/EMP-001.pdf`) — fixed, with a regression test using
exactly that nested-path shape. If a *different* storage key pattern hits the same error, the fix
is the same: confirm `Directory.CreateDirectory` runs before the file write.
