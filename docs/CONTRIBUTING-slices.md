# Adding a vertical slice to Vespera.Application

This is the exact recipe every command/query slice follows. `Features/Departments/` is the
reference implementation — when in doubt, copy its shape. This document assumes you've read
`AGENTS.md` at the repo root; it doesn't repeat the SOLID/coding-standard rules defined there.

## Folder layout

```
Vespera.Application/Features/<BoundedContext>/
    <UseCase>Command.cs              # command record + IRequest<Result<T>>
    <UseCase>CommandValidator.cs     # FluentValidation, one per command
    <UseCase>CommandHandler.cs       # IRequestHandler<TCommand, Result<T>>, one public Handle
    Get<Plural>Query.cs              # query record + its DTO(s), IRequest<Result<T>>
    Get<Plural>QueryValidator.cs     # optional but recommended for paging bounds
    Get<Plural>QueryHandler.cs
    <BoundedContext>MappingConfig.cs # one IRegister per feature, see Mapping/README.md
    <Query>Specification.cs          # concrete ISpecification<T> implementations used by the query handler
```

Everything for one bounded context lives in one flat folder — no `Commands/`/`Queries/`
subfolder split. A reviewer should be able to see an entire use case (command, validator,
handler, and its tests) by looking at file names that share a prefix.

## Naming

- Commands are named for the mutation, imperative mood: `CreateDepartmentCommand`,
  `TransferEmployeeCommand`, `ExitEmployeeCommand`. Never `DepartmentCommand` (not a verb) or
  `DepartmentManager`/`DepartmentHelper` (banned suffixes — enforced by
  `NamingConventionTests`).
- Queries are named for the shape of the answer: `GetDepartmentsQuery` (a list),
  `GetDepartmentByIdQuery` (one record). Not `DepartmentQuery`.
- Validator = `<RequestName>Validator`. Handler = `<RequestName>Handler`. One file each.
- DTOs = `<Entity>Dto` (full) / `<Entity>SummaryDto` (compact, mobile). See
  `Mapping/README.md`.

## The non-negotiables

1. **One public method per handler.** `Handle` only. `MediatRHandlerTests` fails the build
   otherwise — it's SRP, not a style preference.
2. **`Result`/`Result<T>`, never exceptions, for expected failures.** A handler returns
   `Result.Failure(error)` for "department not found" or "code already in use". Exceptions are
   reserved for programmer errors (null args, broken invariants) — see `AGENTS.md`.
3. **Tenant identity always comes from `ITenantContext`, never from the request payload.** A
   command/query must not accept a `TenantId` parameter from the client. Look at
   `CreateDepartmentCommandHandler` — `TenantId` is read from `ITenantContext`, not from
   `CreateDepartmentCommand`.
4. **Handlers stage writes, they never call `SaveChangesAsync`.** Call
   `IWriteRepository<T>.AddAsync`/`Update`/`Remove` to stage the change; `TransactionBehavior`
   is the only place `IUnitOfWork.SaveChangesAsync` gets called, and only when the handler
   returned a successful `Result`. If your handler calls `SaveChangesAsync` directly, something
   is wrong — that dependency shouldn't even be injectable into a handler.
5. **Validation lives in the validator, not the handler.** `ValidationBehavior` runs every
   registered `IValidator<TRequest>` before the handler is invoked. Do not re-check
   `string.IsNullOrWhiteSpace` etc. in the handler for anything the validator already covers —
   Domain-level invariants (e.g. "a department can't be its own parent") still belong in the
   aggregate itself (`Department.Reparent`), since those are true regardless of which command
   calls them.
6. **Retry-safety is opt-in, not assumed.** A mutating command that should be safe to retry
   implements `IIdempotentRequest` and carries an `IdempotencyKey` (see
   `CreateDepartmentCommand`) — that key is expected to arrive as the `Idempotency-Key` HTTP
   header once the API layer exists. `IdempotencyBehavior` short-circuits a duplicate with a
   `Conflict` error before the handler runs a second time. Queries and commands with no
   real-world retry risk simply don't implement the marker.

## The pipeline (why the order is what it is)

`LoggingBehavior → ValidationBehavior → TenantScopeBehavior → IdempotencyBehavior →
TransactionBehavior → PerformanceBehavior`

- **Logging** is outermost so it captures everything, including requests that get rejected by
  every later stage.
- **Validation** runs before tenant/idempotency checks so a malformed request fails fast and
  cheap, before touching anything tenant-scoped.
- **TenantScope** runs before **Idempotency** because the idempotency store is itself
  tenant-scoped — there's no point deduplicating a request that isn't even authorized for a
  tenant.
- **Transaction** wraps the handler and is the only stage after which persistence happens, so
  it sits just outside the handler, after every check that could reject the request for free.
- **Performance** is innermost so its timing reflects the actual handler + save cost, not
  logging/validation overhead.

Every behavior is constrained `where TResponse : Result`, which is what lets
`ResultResponseFactory` build a same-shape failure `TResponse` (`Result` or `Result<T>`)
without the behavior knowing the concrete `T`.

## Persistence: which port to use

- **Writes** always go through `IWriteRepository<T>`. Load the aggregate to mutate via
  `IReadRepository<T>` + a specification (never `IVesperaDbContext` for anything you're about
  to change — you want a tracked entity, not a projection).
- **Reads that return a bounded, moderate-size list** (the common case — most admin screens):
  `IReadRepository<T>.ListAsync`/`CountAsync` with a concrete `ISpecification<T>` colocated in
  the feature folder (see `DepartmentsPagedSpecification`), then `.Adapt<TDto>()` the
  materialized entities.
- **Reads that are heavy or mobile-shaped** (delta sync, cursor-paged lists over a slow
  connection): query `IVesperaDbContext.Set<T>()` directly and `.ProjectToType<TDto>()` so
  only the DTO's columns get selected. Not exercised by the Departments slice; use it when a
  read path is proven to need it, not by default.
- A specification's `CountAsync` implementation applies `Criteria` but **ignores `Paging`** —
  you're counting the full matching set, not one page of it.

## Mapping

See `Mapping/README.md` for the full rationale. Short version: Mapster, one `IRegister` per
feature, and typed IDs (`readonly record struct FooId(Guid Value)`) must be mapped explicitly
with `.Map(dest => dest.Id, src => src.Id.Value)` — Mapster will not unwrap them by convention.

## Testing expectations

Every slice ships with, at minimum:

- `<Command>ValidatorTests.cs` — one `[Theory]`/`[Fact]` per rule, using
  `FluentValidation.TestHelper` (`validator.TestValidate(command).ShouldHaveValidationErrorFor(...)`
  / `ShouldNotHaveValidationErrorFor(...)`). No need to re-test FluentValidation itself, just
  your rules.
- `<Command>HandlerTests.cs` / `<Query>HandlerTests.cs` — every injected port is an
  `NSubstitute` (`Substitute.For<IWriteRepository<Department>>()` etc.), never a real
  implementation and never a mock of the MediatR pipeline. Assert on the returned `Result`
  (`result.IsSuccess`/`result.Error`), not on exceptions. Verify the correct repository/UoW
  calls happened (`await repo.Received(1).AddAsync(...)`).
- Handler tests do **not** re-test pipeline behaviors (tenant scoping, validation,
  idempotency, transactions) — those are cross-cutting and already covered once each in
  `tests/Vespera.Application.UnitTests/Behaviors/`. A handler test only exercises the
  handler's own logic, assuming the pipeline already did its job.

Copy `Features/Departments/*` and `tests/.../Features/Departments/*` file-for-file as your
starting point for a new slice; rename, then adjust the aggregate-specific bits.
