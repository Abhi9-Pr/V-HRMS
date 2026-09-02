# Observability

Structured logs, traces, and metrics all end up in Seq (`http://localhost:8082` when running via
`docker compose up`), but they get there by two different paths — read this before adding a new
log statement or metric so it lands in the right place.

## Serilog (logs) vs. OpenTelemetry (traces/metrics) — why they're split

- **Serilog** (`LoggingServiceCollectionExtensions.AddVesperaLogging`) replaces the default
  `Microsoft.Extensions.Logging` provider entirely and posts structured log events straight to Seq
  via `Serilog.Sinks.Seq` (plus a compact-JSON console sink, useful for container log aggregation
  regardless of Seq). Every existing `ILogger<T>` call in the codebase — including
  `RequestLoggingMiddleware`'s per-request line and every hosted service's `LoggerMessage.Define`
  calls — flows through Serilog automatically; nothing about those call sites changed.
- **OpenTelemetry SDK** (`TelemetryServiceCollectionExtensions.AddVesperaTelemetry`) owns traces
  (ASP.NET Core, HttpClient, EF Core auto-instrumentation) and custom metrics, exported via OTLP to
  the `otel-collector` container, which re-exports to Seq's own OTLP ingestion endpoints
  (`/ingest/otlp/v1/traces`, `/ingest/otlp/v1/metrics` — see `otel-collector-config.yml`). Seq
  2026.1+ ingests OTLP traces and metrics natively and has dashboards over both, so this is the
  only backend in the stack — no separate Prometheus/Grafana.

They're kept apart rather than routing logs through OTel too (a legitimate alternative) because the
codebase already has a mature, working `ILogger`-based logging story (correlation-id scopes,
tenant-aware request logging, PII-scrubbing conventions) that Serilog slots into without touching a
single call site. Re-routing that through OTel logs would mean re-deriving all of that.

## Correlation ids: Angular → API → background jobs

`X-Correlation-Id` already flowed end-to-end from Angular through a single HTTP request (see
`correlation-id.interceptor.ts` and `CorrelationIdMiddleware`) before this phase — every log line
during a request already carried it via `ILogger.BeginScope`, which Serilog picks up automatically
(no `Enrich.FromLogContext()` needed for MS-logging scopes specifically; that call is still present
for genuinely async-flowed properties set via `LogContext.PushProperty`).

What's new is carrying that same id into the **background work a request triggers**:

1. `ICorrelationIdProvider` (`Vespera.Application.Abstractions.Services`) exposes the ambient id —
   null outside an HTTP request. `HttpContextCorrelationIdProvider`
   (`Vespera.Infrastructure/Identity/`) is the implementation, same capture-once-per-scope shape as
   `HttpTenantContext`.
2. Both outbox-write paths (`DomainEventDispatchInterceptor`, `EfOutboxWriter`) stamp
   `OutboxMessageEntity.CorrelationId` from that provider when a message is written.
3. `OutboxDispatcherHostedService` — running in its own DI scope with no ambient HTTP request —
   pushes the *stored* id into its own log scope while dispatching each message.

The result: a request that raises a domain event, and the outbox sweep that processes it minutes
later, both log against the same correlation id. Search Seq for that id and you get the whole
story, not just the synchronous half of it.

## The four dashboards

None of these need bespoke instrumentation per feature — they're all queries/charts over metrics
and log properties that already exist generically:

- **Request latency**: `vespera.request.duration` (OTel histogram, `Vespera` meter), tagged
  `request.name`. Recorded for every MediatR request in `PerformanceBehavior`, regardless of
  whether it was slow — the existing ">500ms" warning log is unchanged and separate.
- **Payroll run duration**: the same `vespera.request.duration` histogram, filtered to
  `request.name` in `{RunDryRunCommand, FinalizePayrollRunCommand, OpenPayrollRunCommand, ...}` —
  not a separate metric. A dedicated payroll-only histogram would just be this filtered view,
  duplicated.
- **Outbox lag**: `vespera.outbox.lag_seconds` (OTel observable gauge) — age of the oldest pending
  outbox row as of the dispatcher's last poll. Zero when the backlog is empty.
- **Failed logins**: `vespera.request.failed` (OTel counter), tagged `request.name` and
  `error.code` — generic to every MediatR request, not login-specific. A failed-logins view is
  this counter filtered to `request.name=LoginCommand`; the same counter also covers every other
  command's failures for free. `LoggingBehavior` also logs a matching warning
  (`{RequestName} failed: {ErrorCode}`) if a log-based view is preferred over the metric.

Building the actual Seq dashboard tiles for these (Settings → Dashboards in the Seq UI, or via its
HTTP API) is a one-time manual step against a running Seq instance, not something committed as a
repo file — dashboard JSON isn't meaningfully diffable/reviewable the way code is, and Seq's
dashboards UI is the faster way to iterate on one anyway.

## Known gaps

- **Seq needs an explicit first-run auth decision.** Seq 2026.1+ refuses to start at all with just
  `ACCEPT_EULA=Y` — it now requires either `SEQ_FIRSTRUN_ADMINPASSWORD` or
  `SEQ_FIRSTRUN_NOAUTHENTICATION` to be set, or the container crash-loops before ever serving HTTP
  (confirmed the hard way: every `curl` and every Serilog/OTLP delivery attempt was silently
  failing against a Seq that had never actually started). `docker-compose.yml` sets
  `SEQ_FIRSTRUN_NOAUTHENTICATION=true`, matching every other service in this dev-only stack (none
  of db/redis/otel-collector have auth either). With no authentication configured, OTLP ingestion
  doesn't need an `X-Seq-ApiKey` header either — Seq's docs describe that header for the general
  case, but an unauthenticated instance accepts ingestion without one.
- **Local (non-compose) `dotnet run`**: `appsettings.Development.json` points the Seq sink and OTLP
  exporter at the compose hostnames (`seq`, `otel-collector`), which won't resolve outside the
  compose network. Both fail silently in the background (Serilog sinks and OTel exporters are
  designed not to crash the host on delivery failure) — console logging still works, telemetry
  just doesn't go anywhere until you're running via compose.
