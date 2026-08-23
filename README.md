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

```bash
dotnet restore
dotnet build
dotnet test
```

## Contributing

Read `AGENTS.md` before making changes — it defines the solution layout, SOLID rules, and
coding standards that the architecture tests enforce.
