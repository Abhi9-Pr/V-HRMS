# 0002. Multi-tenancy: shared database, row-level isolation

**Status:** Accepted

## Context

Vespera serves multiple tenant organizations from one deployment. Every tenant-scoped entity needs
a way to guarantee tenant A's data never leaks into a query run for tenant B — and that guarantee
needs to hold even when a new feature slice is added by someone who has never read this ADR.

## Decision

One shared database, one schema. Every tenant-scoped entity implements `ITenantScoped` (a
`TenantId` property). `VesperaDbContext` registers a global EF Core query filter on every such
entity type, generated once at model-building time
(`Expression.Property(parameter, nameof(ITenantScoped.TenantId))` compared against the ambient
ID), sourced from `ITenantContext` — populated per-request from the authenticated user's token
claim, not from any client-supplied header or route value. A handler that calls
`dbContext.Set<Employee>()` gets tenant isolation for free; there is no per-query opt-in.

## Alternatives considered

- **Database-per-tenant.** Rejected: strongest isolation, but multiplies migration and connection-
  pool operational cost linearly with tenant count, and cross-tenant admin/reporting queries (which
  this system needs — see the platform-admin surface) become distributed queries instead of a
  `WHERE` clause.
- **Schema-per-tenant** (one Postgres schema per tenant, shared database). Rejected: better than
  DB-per-tenant operationally, but still requires dynamic connection/schema switching per request
  and migrations applied per-schema — meaningfully more moving parts than a query filter, for
  isolation guarantees this system doesn't need at its current tenant count.
- **Manual `WHERE TenantId = @tenantId` on every query, no global filter.** Rejected outright: this
  is a guarantee that depends on every contributor remembering to add the clause, every time,
  forever. A single missed clause is a cross-tenant data leak — exactly the failure mode a global
  filter is designed to make structurally impossible instead of procedurally hoped-for.

## Consequences

- Tenant isolation is a property of the DbContext model, not of individual query code — a new
  feature slice gets it automatically as long as its entity implements `ITenantScoped`.
- The guarantee is only as strong as `ITenantContext` itself: if that's ever populated from
  something client-controlled (a header, a query string) rather than the verified auth token
  claim, the filter becomes worthless. This is exactly the class of bug the cross-tenant IDOR test
  suite (see `docs/security/asvs-checklist.md`) exists to catch.
- Any code path that legitimately needs to see across tenants (platform admin tooling, the
  dev-seeder) has to explicitly bypass the filter (`IgnoreQueryFilters()` or an admin-specific
  repository interface) — a deliberate, visible escape hatch rather than an accidental one.
- A single noisy-neighbor tenant (e.g. a very large employee count) shares the same database
  resources as every other tenant; there's no per-tenant scaling knob without moving to one of the
  rejected alternatives later.
