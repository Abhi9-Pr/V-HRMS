# API Mobile Contract

Phase 4 ships four pieces of mobile-facing plumbing. All four are functional and tested in
isolation, but none is wired into a concrete feature endpoint yet — which endpoints adopt them is
a later-phase decision (per AGENTS.md: "do not scaffold future phases"). This document is the
contract a mobile client (and whichever phase wires up the first consumer) can rely on.

## 1. Idempotency-Key replay

**What it does.** A repeated mutating request (POST/PUT/PATCH/DELETE) carrying the same
`Idempotency-Key` header gets back the *exact original response* — status code, content type, and
body — without the handler running a second time.

**Contract.**
- Header: `Idempotency-Key: <opaque client-generated string, e.g. a UUID>`. Optional — omitting it
  means "no idempotency guarantee," exactly like today.
- Only applied to mutating methods (GET/HEAD are never buffered or replayed).
- A cached response is kept for 24 hours, then eligible for reuse of the same key.
- Only successful (2xx) responses are cached — a failed request can be safely retried with the
  same key without getting stuck replaying an error.

**How it's implemented.** `Vespera.Api.Middleware.IdempotencyMiddleware`, backed by
`IIdempotencyResponseCache`/`EfIdempotencyResponseCache` (a small dedicated table,
`IdempotencyResponses`). This is deliberately **separate** from Phase 3's `IIdempotencyStore`
(used by `IdempotencyBehavior` in the MediatR pipeline): that one only ever recorded "this command
was processed," with no response body to replay. The two are independent, complementary
mechanisms — a command can be protected by neither, either, or both.

**Client guidance.** Generate a fresh key per logical user action (e.g. once when a "Submit" button
is tapped), and reuse that exact key for any automatic retry of that same action (e.g. after a
network timeout). Do not reuse a key across two different logical actions.

## 2. ETag / If-None-Match for reference data

**What it does.** Lets a client that already has a cached copy of slow-changing reference data
(departments, designations, leave types, ...) avoid re-downloading it when nothing changed.

**Contract.**
- A GET response for a reference-data resource carries an `ETag: "<version-tag>"` header.
- A client sends that tag back as `If-None-Match: "<version-tag>"` on the next request.
- If the tag still matches, the server responds `304 Not Modified` with no body.

**How it's implemented.** `Vespera.Api.Http.ETagNegotiation.TryShortCircuit(HttpContext, string tag)`
— call it with a stable version stamp for the resource (e.g. the entity's `RowVersion` or
`ModifiedAt`, hex/base64-encoded) at the top of a query handler/endpoint; if it returns `true`, the
304 has already been written and the caller should return immediately without touching the body.

## 3. Compact-DTO content negotiation

**What it does.** Lets a mobile client ask for the smaller `*SummaryDto` shape instead of the full
`*Dto` (see `CONTRIBUTING-slices.md`'s Dto/SummaryDto convention) without needing a second endpoint
or query parameter per resource.

**Contract.** Header: `X-Response-Shape: compact`. Absent or any other value means "full shape,"
the current default for every existing endpoint.

**How it's implemented.** `Vespera.Api.Http.ICompactResponseContext`
(`CompactResponseContext`), a scoped service reading the header once per request. A query handler
or Mapster profile can inject/consult it and choose which `IRegister` mapping (`Dto` vs
`SummaryDto`) to project onto. This is the negotiation *signal* only — it doesn't change any
existing query's output yet.

## 4. Delta sync

**What it does.** Gives a mobile client an efficient "what changed since I last synced" query
shape instead of re-fetching a full list every time, with tombstones for records deleted since.

**Contract (request).** `since` (ISO-8601 timestamp of the last successful sync), optional `cursor`
(opaque, from a previous response's `nextCursor`, for paging through a large change set) and
`pageSize` (default 100) — see `Vespera.Application.Common.DeltaSyncRequest`.

**Contract (response).** `upserts` (created-or-changed records, in the endpoint's normal DTO
shape), `tombstonedIds` (ids of records deleted since `since` — the client should remove these
locally), `syncedAt` (the server's clock at response time — pass this back as the next `since`),
and `nextCursor` (non-null if there's more to page through) — see
`Vespera.Application.Common.DeltaSyncResult<T>`.

**How it's implemented.** `Vespera.Application.Common.DeltaSyncQueryHandlerBase<TRequest,TEntity,TDto>`
— an abstract base handler built entirely on existing ports (`IReadRepository<T>`,
`ISpecification<T>`); a concrete feature slice supplies the entity-specific specification (the
"changed since" filter), id extraction, and DTO mapping. Soft-deleted rows must still be
queryable (as tombstones), so a concrete implementation typically needs `IReadRepositoryAdmin<T>`
rather than the ordinary tenant/soft-delete-filtered `IReadRepository<T>` — see that interface's
own doc comment.
