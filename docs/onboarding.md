# Onboarding

Everything you need is already documented somewhere in `docs/` — this page is the reading order
and the map, not a duplicate of any of it. Read `AGENTS.md` too; it's short and it's the rule set
the architecture tests actually enforce.

## Day one: get it running

```bash
git clone <repo>
cd vespera
docker compose up -d --build
```

Wait for `docker compose ps` to show `api` and `client` healthy, then open
`http://localhost:4200`. Tenant code `DEMO` is already seeded with working demo users — see
`docs/CONTRIBUTING-frontend.md` for the actual credentials and who each seeded user is (a mix of
HR, Finance, and regular-employee roles, since several features gate on role/permission and you'll
want to log in as the right one to see them).

That's Option A from the root `README.md`. Option B (running the API and Angular dev server
directly, without Docker) is faster for active development — `docs/CONTRIBUTING-frontend.md`
covers it, including the one gotcha that trips up almost everyone the first time: `vespera-shared`
(the Angular workspace library the app's API client and shared types live in) has to be built
(`ng build vespera-shared`) before the main app can resolve it, and isn't rebuilt automatically by
the dev server's file watcher when you change its source.

## Understand the shape of the system

1. Root `README.md`'s "Solution layout" section — the four .NET projects and the dependency rule.
2. [`docs/adr/`](adr/) — *why* the system is shaped this way, not just what the shape is. Start
   with [0001](adr/0001-clean-architecture-cqrs.md) (Clean Architecture + CQRS) and
   [0002](adr/0002-multi-tenancy-shared-db-row-level-isolation.md) (multi-tenancy) — those two
   explain the two structural decisions everything else sits on top of.
3. `docs/spec/vespera-master-spec.md` — the full feature specification, if you need to know what a
   feature is *supposed* to do before touching it.

## "How do I...?"

| I want to... | Read |
| --- | --- |
| Add a new backend feature slice (command/query/handler/validator) | `docs/CONTRIBUTING-slices.md` |
| Work on the Angular client, understand its state management | `docs/CONTRIBUTING-frontend.md`, `docs/frontend-state.md` |
| Run the test suites, understand the coverage/mutation gates | `docs/testing.md`, [ADR 0007](adr/0007-testing-depth-coverage-gates-payroll-mutation.md) |
| Add a log line, a trace, or a metric | `docs/observability.md`, [ADR 0006](adr/0006-observability-serilog-otel-split.md) |
| Understand `docker-compose.yml` vs. the dev auto-provisioner | `docs/docker-compose.md` |
| Build a feature that consumes the API from `Vespera.Mobile` | `docs/mobile-integration-guide.md`, `docs/api-mobile-contract.md` |
| Understand the CI pipeline, image tags, or the staging/production gates | `docs/deployment.md`, [ADR 0005](adr/0005-deployment-target-ghcr-docker-compose.md) |
| Diagnose or recover a running deployment | `docs/runbook.md` |
| Handle data subject requests, encryption, retention | `docs/data-protection.md` |
| Understand the auth model, TOTP step-up, rate limits | [ADR 0004](adr/0004-auth-jwt-refresh-step-up-totp.md), `docs/security/` |
| Add support for a new biometric attendance device vendor | `docs/adding-a-biometric-vendor.md` |
| Understand a specific performance fix or run the k6 load tests | `docs/performance.md` |

## Before you open a PR

- `AGENTS.md`'s SOLID rules are enforced by `Vespera.Architecture.Tests`, not by convention — a
  violation fails the build, so you'll find out immediately, not in review.
- `dotnet build`, `dotnet test`, and (for client changes) `npm run lint` / `npx jest` all need to be
  clean — `.github/workflows/ci.yml` runs the same checks (plus coverage gates, mutation testing on
  payroll, and a security scan) on every PR.
- If you're touching a critical user journey with client-visible behavior, check whether
  `Vespera.Client/e2e/` already has (or should have) a Playwright spec for it — see `docs/testing.md`.
