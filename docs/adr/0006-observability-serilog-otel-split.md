# 0006. Observability: Serilog for logs, OpenTelemetry for traces/metrics

**Status:** Accepted

## Context

Diagnosing a production issue needs three different shapes of signal: structured log lines with
rich context (what a specific request/handler did), distributed traces (how a request's time was
actually spent across handler → EF Core → external calls), and metrics (aggregate rates/latencies
over time). One instrumentation library rarely does all three equally well, and this system
already leans on `LoggerMessage.Define<...>`-style structured logging as a hard convention
(`CA1848` is a build error — see `Vespera.Application/Behaviors/PerformanceBehavior.cs`).

## Decision

Two independent pipelines, both landing in Seq (`http://localhost:8082` via docker-compose) but
via different paths, per `docs/observability.md`. Serilog
(`LoggingServiceCollectionExtensions.AddVesperaLogging`) fully replaces the default
`Microsoft.Extensions.Logging` provider and posts structured events straight to Seq via
`Serilog.Sinks.Seq`, plus a compact-JSON console sink for container log aggregation — every
existing `ILogger<T>` call site (including every `LoggerMessage.Define` call, like the dashboard
handler's per-widget-failure log added when its concurrency bug was diagnosed) flows through this
automatically. The OpenTelemetry SDK
(`TelemetryServiceCollectionExtensions.AddVesperaTelemetry`) separately owns traces and metrics,
exported via OTLP to the `otel-collector` service — the signal a request-latency or call-count
regression (like the N+1 queries and query-translation bug the k6 load tests surfaced, see
`docs/performance.md`) shows up in, distinct from the structured-log signal Serilog carries.

## Alternatives considered

- **OpenTelemetry Logs API for everything**, dropping Serilog. Rejected: OTel's logging story is
  younger and less ergonomic than Serilog's structured-logging API for a codebase already built
  around `ILogger<T>` and the `LoggerMessage.Define` convention; switching would mean rewriting
  every existing log call site for no functional gain, since Seq is the destination either way.
- **Serilog for everything, including traces/metrics** (via Serilog enrichers/sinks that
  approximate tracing). Rejected: Serilog isn't a tracing system — it has no native concept of a
  span or a distributed trace context propagated across service boundaries, which is exactly what
  the performance investigations in `docs/performance.md` needed.

## Consequences

- Two configuration surfaces to keep aligned (`AddVesperaLogging` and `AddVesperaTelemetry`) rather
  than one — a contributor adding a new signal has to know which of the two APIs it belongs to.
  `docs/observability.md` exists specifically so that choice doesn't have to be rediscovered per
  contributor.
- Both are non-blocking if their sink is unreachable (buffered/dropped, never crashes the app) — a
  slow or down Seq/otel-collector never holds up a request, but also means signal loss during an
  outage is silent unless someone is watching for it.
- The dashboard concurrency bug found during this engagement's testing phase (`Task.WhenAll` over
  a shared scoped `DbContext`, previously failing silently) was only diagnosable at all once
  `GetDashboardQueryHandler` had proper `LoggerMessage.Define`-based logging added — a direct,
  concrete example of Serilog's half of this split earning its keep, not a hypothetical.
- Both pipelines being non-blocking-if-unreachable also means a regression in either's own wiring
  degrades real incident-response capability silently — nothing fails loudly just because logs or
  traces stopped arriving.
