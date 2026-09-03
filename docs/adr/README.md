# Architecture Decision Records

An ADR records a decision that shaped the system, the alternatives it was weighed against, and
what living with it actually costs — not a tutorial on how to use the result (that's what the rest
of `docs/` is for). Each one links out to the implementation doc that covers the how, rather than
duplicating it.

## Index

| ADR | Decision |
| --- | --- |
| [0001](0001-clean-architecture-cqrs.md) | Clean Architecture layering + CQRS via MediatR |
| [0002](0002-multi-tenancy-shared-db-row-level-isolation.md) | Multi-tenancy: shared database, row-level isolation |
| [0003](0003-dual-idempotency-mechanisms.md) | Two independent idempotency mechanisms, not one |
| [0004](0004-auth-jwt-refresh-step-up-totp.md) | Auth: JWT + refresh tokens, step-up TOTP for Finance |
| [0005](0005-deployment-target-ghcr-docker-compose.md) | Deployment target: GHCR + docker-compose, no cloud host |
| [0006](0006-observability-serilog-otel-split.md) | Observability: Serilog for logs, OpenTelemetry for traces/metrics |
| [0007](0007-testing-depth-coverage-gates-payroll-mutation.md) | Testing depth: tiered coverage gates, payroll-scoped mutation testing |
| [0008](0008-dotnet-aspire-evaluation.md) | .NET Aspire: deferred, not adopted |

## Writing a new one

Copy the shape of any existing ADR: **Status** (Accepted/Superseded, with a date), **Context**
(the problem, in enough detail that the decision looks inevitable once stated), **Decision** (one
or two sentences, stated plainly), **Alternatives considered** (the real ones, with why they lost
— not a straw man), **Consequences** (what this costs or constrains going forward, not just what
it buys). Number sequentially; never renumber or delete a superseded one — add a new ADR that
supersedes it and say so in both directions.
