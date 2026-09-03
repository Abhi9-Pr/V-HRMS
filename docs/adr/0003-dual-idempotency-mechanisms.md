# 0003. Two independent idempotency mechanisms, not one

**Status:** Accepted

## Context

A mutating request can be retried for two very different reasons, at two very different layers.
Internally, `IdempotencyBehavior` sits in the MediatR pipeline (`Vespera.Application/Behaviors/`)
and needs to know "has this exact command already been processed?" before a handler runs a second
time — e.g. a payroll computation triggered twice by a flaky background job. Externally, an HTTP
client (particularly a mobile client on an unreliable connection — see
`docs/api-mobile-contract.md`) needs "give me back the *exact original HTTP response*" for a
repeated request carrying the same `Idempotency-Key` header, including status code and body, not
just "don't run it twice."

## Decision

Keep them as two separate mechanisms rather than unifying into one. `IIdempotencyStore` (used by
`IdempotencyBehavior`) records only that a given command was processed — a marker, no response
body. `IIdempotencyResponseCache`/`EfIdempotencyResponseCache` (used by
`Vespera.Api.Middleware.IdempotencyMiddleware`, its own dedicated `IdempotencyResponses` table)
caches the full HTTP response — status, content type, body — for 24 hours, keyed by the
client-supplied `Idempotency-Key` header, and only for successful (2xx) responses. A command can be
protected by neither, either, or both, depending on whether it needs internal replay-prevention,
client-facing response replay, or both.

## Alternatives considered

- **One unified idempotency mechanism**, always caching the full response body. Rejected: the
  MediatR-pipeline use case (protecting a handler from double-processing when invoked from a
  non-HTTP context — a scheduled job, a domain-event handler) has no HTTP response to cache in the
  first place; forcing every idempotency-protected command through an HTTP-shaped cache would leak
  a web concern into the Application layer, which `Vespera.Domain`/`Vespera.Application`'s
  dependency rule (ADR-0001) specifically keeps free of.
- **HTTP-layer only** (drop `IIdempotencyStore`, rely solely on the middleware). Rejected: several
  commands need replay-protection when invoked from outside an HTTP request entirely (Hangfire
  jobs, domain-event reactions), where there's no `Idempotency-Key` header to key off of.

## Consequences

- A contributor adding idempotency protection to a new command has to consciously choose which
  mechanism(s) apply — this is a real decision, not a checkbox, and the wrong choice (e.g. only the
  MediatR-level marker on a command whose HTTP caller needs the exact original response replayed)
  fails silently: the second request succeeds again with a *new* response rather than erroring.
- Two idempotency tables/stores to reason about instead of one — see `docs/api-mobile-contract.md`
  section 1 for the client-facing contract of the response-cache half.
- Only the HTTP-layer cache understands "successful responses only, 24-hour window" — the MediatR
  marker's own retention/eligibility rules are a separate, independently-tunable policy.
