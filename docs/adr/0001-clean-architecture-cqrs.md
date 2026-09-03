# 0001. Clean Architecture layering + CQRS via MediatR

**Status:** Accepted

## Context

Vespera is a multi-tenant enterprise HRMS: payroll, leave, attendance, recruitment, helpdesk,
assets — a wide surface, built by however many contributors touch it over the system's life, where
the cost of a wrong dependency creeping into the wrong layer compounds silently until someone tries
to unit-test a business rule and discovers it's wired to `HttpContext`.

## Decision

Four projects, dependencies pointing inward only: `Vespera.Domain` (entities, value objects,
domain events — zero external dependencies, no EF, no MediatR, no ASP.NET) ← `Vespera.Application`
(CQRS commands/queries as MediatR contracts, DTOs, FluentValidation validators, port interfaces) ←
`Vespera.Infrastructure` (EF Core, Identity, provisioning, email/SMS/push, OCR, storage, Hangfire —
implements Application's ports) ← `Vespera.Api` (controllers, middleware, SignalR hubs, the DI
composition root). Every use case is one MediatR command or query handler with a single public
`Handle` method — no multi-purpose "Service" classes. `Vespera.Architecture.Tests` enforces the
dependency direction and these SOLID rules in code, so a violation fails the build, not code
review.

## Alternatives considered

- **A conventional layered/N-tier structure** (Controllers → Services → Repositories, services
  free to depend on each other and on infrastructure types directly). Rejected: this is exactly
  the shape that lets business rules quietly depend on `DbContext` or `HttpContext`, which is what
  Clean Architecture's inward-only rule specifically prevents.
- **A single project with folder-based separation** instead of four assemblies. Rejected: folder
  boundaries are conventions a contributor can violate with an unnoticed `using` statement; project
  references are a compiler-enforced boundary, and `Vespera.Architecture.Tests` can assert against
  them mechanically.
- **Vertical slice architecture without a Domain/Application split** (each feature owns its own
  types top to bottom, no shared entity model). Rejected for an HRMS specifically: employee,
  payroll, and leave data are too deeply relational and rule-heavy (see `AGENTS.md`'s SOLID rules)
  to duplicate a shared domain model per slice without it drifting.

## Consequences

- A new feature always touches at least three of the four projects (Application for the
  command/query + handler + validator, Infrastructure only if a new port implementation is
  needed, Api for the controller action) — more ceremony per feature than a single-project
  approach, deliberately traded for the dependency guarantee.
- Domain logic is trivially unit-testable with no mocking of infrastructure — see the Domain test
  suite's own size (778 tests) relative to its near-100% coverage gate.
- `Vespera.Architecture.Tests` failing is a hard stop, not a lint warning that can be silenced —
  this is intentional, but it means the rule set in `AGENTS.md` has to stay accurate, or the
  architecture tests themselves become the source of confusing false failures.
