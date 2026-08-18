# Vespera HRMS — Agent Operating Rules

## Project
Vespera is a multi-tenant enterprise HRMS. Backend: .NET 8, Clean Architecture + DDD.
Frontend: Angular 18 SPA. A Capacitor mobile app will consume the same API later.
Full feature specification lives in `/docs/spec/vespera-master-spec.md`.

## Solution layout (do not deviate)
Vespera.Domain          — entities, value objects, enums, domain events, domain services.
                          Zero external dependencies. No EF, no MediatR, no ASP.NET.
Vespera.Application     — CQRS commands/queries (MediatR), DTOs, validators, interfaces
                          (ports). Depends only on Domain.
Vespera.Infrastructure  — EF Core, Identity, provisioning, email/SMS/push, OCR, storage,
                          Hangfire. Implements Application ports. Depends on Application.
Vespera.Api             — controllers, middleware, SignalR hubs, DI composition root.
Vespera.Client          — Angular workspace.
Vespera.Mobile          — Capacitor shell (added in Phase 13).
tests/                  — Domain.UnitTests, Application.UnitTests, Api.IntegrationTests,
                          Architecture.Tests.

Dependency rule: dependencies point inward only. Architecture tests enforce this.

## SOLID rules — these are hard requirements, not aspirations
- SRP: one MediatR handler per use case, one public method (`Handle`). No "Service" classes
  with more than one reason to change. If a class name contains "Manager" or "Helper",
  it is wrong.
- OCP: extension via new implementations + DI registration. Adding a payroll component, a
  notification channel, a DB provisioner or an OCR provider must not modify existing files
  beyond one registration line.
- LSP: no implementation may throw NotImplementedException or narrow a contract's accepted
  inputs. If a subtype can't honour the interface, split the interface.
- ISP: small role interfaces (IReadRepository<T> / IWriteRepository<T>, ICanApprove,
  INotificationChannel). Never one fat IRepository with 20 members.
- DIP: interfaces declared in Application, implemented in Infrastructure. Constructor
  injection only. No service locator, no `new` on a dependency, no static state.

## Coding standards
- Nullable enabled, warnings as errors, `TreatWarningsAsErrors=true`, latest C#.
- `Result<T>` / `Result` for expected failures; exceptions only for programmer error.
- Validation via FluentValidation in a MediatR pipeline behavior — never inside handlers.
- All money is `Money` (decimal + currency). `double`/`float` are banned in Domain.
- All persisted timestamps are UTC (`DateTimeOffset`). Display timezone is a user setting.
- Every mutating endpoint accepts an `Idempotency-Key` header where retries are plausible.
- Every entity: `TenantId`, `CreatedAt/By`, `ModifiedAt/By`, `RowVersion`, soft-delete flag.
- No `DbContext` in controllers. No business logic in controllers. Controllers only:
  bind -> send MediatR message -> map Result to HTTP.
- xUnit + FluentAssertions + NSubstitute. Every command handler gets a test.

## Working agreement
- Work only on the phase I give you. Do not scaffold future phases.
- Before writing code, restate your plan as a file-by-file list and wait for my "go".
- After the phase, run `dotnet build`, `dotnet test`, and `npm run lint` where applicable,
  and report results. Do not declare done on a red build.
- Prefer small, verifiable commits with conventional-commit messages.
- If a requirement in the spec conflicts with these rules, stop and ask. Do not guess.
- Never commit secrets. Local secrets go in user-secrets / `.env` (gitignored).