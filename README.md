# Vespera HRMS

Vespera is a multi-tenant enterprise HRMS.

- **Backend:** .NET 8, Clean Architecture + DDD.
- **Frontend:** Angular 18 SPA (`Vespera.Client`).
- **Mobile:** Capacitor shell (`Vespera.Mobile`, added in Phase 13).

The full feature specification lives in [`docs/spec/vespera-master-spec.md`](docs/spec/vespera-master-spec.md).
Agent/contributor operating rules live in [`AGENTS.md`](AGENTS.md).

## Solution layout

```
Vespera.Domain          entities, value objects, enums, domain events, domain services.
                         Zero external dependencies. No EF, no MediatR, no ASP.NET.
Vespera.Application     CQRS commands/queries (MediatR contracts), DTOs, validators,
                         interfaces (ports). Depends only on Domain.
Vespera.Infrastructure  EF Core, Identity, provisioning, email/SMS/push, OCR, storage,
                         Hangfire. Implements Application ports. Depends on Application.
Vespera.Api             controllers, middleware, SignalR hubs, DI composition root.
Vespera.Client          Angular 18 SPA — see docs/CONTRIBUTING-frontend.md.
tests/
  Vespera.Domain.UnitTests
  Vespera.Application.UnitTests
  Vespera.Api.IntegrationTests
  Vespera.Architecture.Tests   enforces the dependency and SOLID rules below in code.
```

Dependencies point inward only: `Api -> Infrastructure -> Application -> Domain`.
`Vespera.Architecture.Tests` fails the build if this rule (or the other SOLID rules in
`AGENTS.md`) is violated.

## Getting started

### Option A — Docker only

Requires nothing but Docker (with Compose v2, bundled with Docker Desktop and modern Docker
Engine):

```bash
docker compose up -d --build
```

Wait for `docker compose ps` to show `api` and `client` as healthy, then open
`http://localhost:4200` — the demo tenant (code `DEMO`) is already seeded; see
`docs/CONTRIBUTING-frontend.md` for the seeded login credentials. See `docs/docker-compose.md`
for how this stack relates to the auto-provisioner used by Option B below — the two must not be
pointed at the same database.

### Option B — running the API and client directly

```bash
dotnet restore
dotnet build
dotnet test
```

Then follow "Running the app against the real API" in `docs/CONTRIBUTING-frontend.md`.

## Contributing

Read `AGENTS.md` before making changes — it defines the solution layout, SOLID rules, and
coding standards that the architecture tests enforce.
