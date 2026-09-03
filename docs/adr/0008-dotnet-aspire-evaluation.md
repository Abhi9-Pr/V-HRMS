# 0008. .NET Aspire: deferred, not adopted

**Status:** Deferred (evaluated 2026-09; not Accepted, not Rejected outright — see "Revisit
trigger" below)

## Context

.NET Aspire is Microsoft's opinionated, C#-based dev-time orchestration model: an `AppHost`
project declares resources (`AddProject<T>()` for .NET projects, `AddNpmApp()` for a Node/Angular
frontend, `AddContainer()`/`AddDockerfile()` for everything else) and Aspire wires up automatic
service discovery, connection-string injection, and a built-in local dashboard (structured logs,
distributed traces, resource health) with comparatively little configuration. It's a genuine
candidate here because Vespera already has exactly the kind of multi-service local dev stack
(api, client, db, redis, seq, otel-collector) Aspire targets, *and* because `docs/docker-compose.md`
already documents a real, named pain point: two independent, easy-to-confuse ways to get a local
Postgres (the Phase 3 in-process auto-provisioner and `docker-compose.yml`'s own `db` service),
explicitly flagged as "the most likely way a new developer wastes an afternoon."

## Decision

Evaluated, not adopted, for now. Aspire is confirmed (via its own migration documentation) to be a
**development-time tool only** — production deployment remains the adopter's own responsibility
either way, meaning it would sit alongside, not replace, the docker-compose-based CI/staging/
production pipeline [ADR 0005](0005-deployment-target-ghcr-docker-compose.md) already established
in Phase 6. Its real, specific benefit to this codebase — collapsing the auto-provisioner-vs-
compose confusion into one unified orchestration surface for local dev — is genuine, but that
confusion already has a working (if imperfect) mitigation: `docs/docker-compose.md` itself, written
specifically to prevent exactly this mistake. Adding a third orchestration concept to a system
that already works end-to-end, for a benefit an existing doc already substantially covers, isn't
worth the migration cost right now.

## Alternatives considered

- **Adopt Aspire fully, replacing both the Phase 3 auto-provisioner and `docker-compose.yml` for
  local dev.** The strongest case, and the one this ADR came closest to accepting — but still only
  addresses local dev; `docker-compose.staging.yml` and the GHCR-based CI pipeline (ADR 0005) would
  be untouched regardless, so this isn't a full replacement of the existing orchestration story,
  only its local-dev third. Deferred rather than accepted: the cost (a new AppHost project, a new
  C#-based DSL to learn alongside the YAML one already in place, re-validating that seeded demo
  data still seeds correctly through a new startup path) is being paid now, on a mature system,
  for a problem `docs/docker-compose.md` already mitigates.
- **Adopt Aspire only for its dashboard**, layered on top of the existing docker-compose stack
  without an AppHost. Rejected: Aspire's dashboard is a feature of AppHost-orchestrated resources,
  not a standalone tool that attaches to an unrelated docker-compose stack — this isn't actually a
  smaller, separable adoption, it implies the fuller migration above.
  Vespera already has Seq + an OTel collector wired up ([ADR 0006](0006-observability-serilog-otel-split.md)),
  which covers the same structured-logs/traces need in a way that also matches production, unlike
  Aspire's dashboard, which is dev-only.
- **Reject outright, revisit never.** Rejected as too strong a stance: the identified benefit (real
  service-discovery/connection-string simplification, and a genuine fix for a documented confusion
  point) is real enough that closing the door permanently would be premature.

## Consequences

- No change to any existing project, workflow, or doc as a result of this ADR — `docker-compose.yml`,
  the Phase 3 auto-provisioner, and `.github/workflows/ci.yml` all remain exactly as ADR 0005 left
  them.
- The auto-provisioner-vs-compose confusion `docs/docker-compose.md` warns about remains mitigated
  by documentation, not eliminated by tooling — a real, if smaller, residual onboarding risk.
- **Revisit trigger:** if `docs/docker-compose.md`'s warning keeps failing to prevent the mistake
  it's written for (i.e., new contributors keep pointing the wrong mechanism at the wrong
  database despite reading it), that's the concrete signal to re-open this ADR and scope a real
  Aspire AppHost migration for local dev specifically — not a periodic "is Aspire mature yet"
  check, a signal tied to an actual recurring cost.

Sources: [.NET Aspire migrate-from-docker-compose guide](https://aspire.dev/app-host/migrate-from-docker-compose/), [.NET Aspire vs Docker Compose local dev orchestration](https://codingdroplets.com/dotnet-aspire-vs-docker-compose-local-dev-orchestration-2026), [Adding an Angular project to .NET Aspire](https://timdeschryver.dev/blog/how-to-include-an-angular-project-within-net-aspire).
