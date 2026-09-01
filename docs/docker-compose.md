# docker-compose.yml vs. the Phase 3 dev auto-provisioner

Vespera now has **two independent ways to get a Postgres database during development**, and they
must not be pointed at each other. Confusing them is the most likely way a new developer wastes an
afternoon, so read this before touching either.

## The two mechanisms

**1. The Phase 3 auto-provisioner** (`Vespera.Infrastructure/Provisioning/`, wired up in
`Vespera.Api/Extensions/DatabaseServiceCollectionExtensions.cs`). This runs *inside the API
process* itself, the moment you `dotnet run --project Vespera.Api` or `F5` from an IDE, with no
compose file involved at all. Controlled by `Vespera:Database:Mode` (`Auto` by default in
Development — see `appsettings.Development.json`): it tries an external connection string first,
then a locally-running Postgres, then (`Mode=Docker`/the Docker candidate in `Auto`) it will
`docker run` its own labeled, self-managed Postgres container (`vespera-db-dev`, host port
`55432` by default), then finally falls back to SQLite. `tools/vespera-db.ps1`/`.sh` are thin
lifecycle wrappers (up/down/reset/status/logs) around that same self-managed container. This is
the path for "I'm running the API directly on my machine and just want *a* database, I don't care
which."

**2. `docker-compose.yml`** (repo root) — the full stack: `db`, `redis`, `seq`, `otel-collector`,
`api`, `client`, brought up together with `docker compose up -d`. This is the path for "I want the
*whole app* running, including the Angular client and the API talking to its own Postgres, on a
machine that only has Docker installed" — onboarding, demos, and Playwright/CI runs against a real
stack. The `api` service in this compose file sets `Vespera__Database__Mode=External` explicitly,
so the API never tries to self-provision anything — it always talks to compose's own `db` service.

## The rule

**Never let the API self-provision (`Mode=Auto` or `Mode=Docker`) while also running it inside
docker-compose, and never point docker-compose's `api` service at the auto-provisioner's
self-managed container.** Practically:

- Running the app via `docker compose up`: the `api` service's `Vespera__Database__Mode=External`
  env var already takes care of this — don't remove it, and don't add a bind mount or config
  override that changes `Vespera:Database:Mode` back to `Auto`/`Docker` for that service.
- Running the app via `dotnet run`/`ng serve` (not compose): don't also have `docker compose up`
  running against the same repo at the same time unless you've deliberately set
  `Vespera:Database:Mode=External` with a connection string pointed at compose's `db` (host port
  `55433`, offset from the auto-provisioner's `55432` specifically so the two can coexist on disk
  without a port clash — but they're still two different databases with two different seed
  states unless you point one at the other on purpose).
- If you're unsure which database you're talking to: `Vespera:Database:Mode` in the effective
  configuration (environment variables win) tells you. `External` = compose (or your own Postgres).
  `Auto`/`Docker`/`Local` = the self-provisioner.

## Why the `api` service runs `ASPNETCORE_ENVIRONMENT=Development`, not `Production`

This surprises people coming from "compose = prod-like environment" conventions. Here it's
deliberate: `docker-compose.yml` is a **local/CI sandbox**, not a production deployment
simulation. Development mode keeps today's startup behavior — automatic `dotnet ef` migration and
`DevelopmentSeeder` demo-data seeding — which is exactly what "clone, run one command, reach a
seeded working application" requires. A real production deploy uses
`ASPNETCORE_ENVIRONMENT=Production`, which **does not** auto-migrate (see
`DatabaseServiceCollectionExtensions.InitializeVesperaDatabaseAsync`) and does not seed demo data;
schema changes are applied out-of-band via the migration bundle
(`tools/publish-migrations-bundle.ps1`/`.sh`) as a discrete deploy step, before the new API image
is rolled out. Building an actual staging/production deployment pipeline around that bundle is a
separate, later piece of work — this compose file and the Production migration gate are both
prerequisites for it, not the pipeline itself.

## Seq and the OTel collector

`seq` (UI at `http://localhost:8081`) and `otel-collector` are stood up in the compose stack but
are currently **inert** — nothing in the API emits structured logs or OTLP traces/metrics yet.
`otel-collector-config.yml` is a placeholder (OTLP in, `debug` exporter out) so `docker compose up`
succeeds cleanly. A later observability phase adds Serilog (with a Seq sink) and the OpenTelemetry
SDK to `Vespera.Api`, at which point these containers start receiving real data without any
compose changes needed. Seq's UI is published on host port `8082` (not the more obvious `8081`) —
that port is commonly already bound by other local tooling.

## Known limitation: OCR

`redis` is provisioned but not yet wired into the app (no `IDistributedCache` consumer exists —
`MemoryDashboardWidgetCache` is still in-memory). OCR (`Vespera:Ocr:Provider=Tesseract`) needs
`.traineddata` language files on disk at `Vespera:Ocr:TesseractDataPath`; those aren't bundled into
the API image, so OCR requests will fail in the compose stack until a `tessdata` volume is mounted.
Neither blocks the core seeded happy path (login, dashboard, departments, etc.).
