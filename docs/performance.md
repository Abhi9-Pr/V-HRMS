# Performance

How Vespera's performance work was actually done: what was seeded, what load tests exist, what
they found, and what changed as a result. This is a record of findings, not a tuning guide —
re-run the load tests against a freshly seeded stack before trusting any number here as current.

## Seeding a load-test dataset

`k6/seed-employees.js` imports employees through the real bulk-import HTTP pipeline (the same
`POST /api/v1/employees/import` a real HR admin uses), not a direct-DB script — so it exercises
the actual code path, not a shortcut around it. It provisions its own suffixed departments/
designations/locations first (avoiding collisions with any earlier run's master data), then
imports in chunks.

```
./tools/run-load-tests.ps1 seed          # 5,000 employees (default)
./tools/run-load-tests.ps1 seed 500      # or a smaller count
```

Verified at full scale: 5,000/5,000 employees imported successfully in 52.7s against a local
docker-compose stack.

## Load tests

All three scripts live in `k6/` and run via `tools/run-load-tests.{sh,ps1}` against an
already-running `docker compose up -d` stack (k6 is a one-shot container, not a compose service —
see that script's own header). Each expects `seed-employees.js` to have been run first for a
realistic employee count, except `payroll-dry-run.js`, which seeds its own smaller, self-contained
cohort (see that file's header for why).

| Script | Exercises | Notes |
|---|---|---|
| `dashboard.js` | Dashboard widget endpoints | Ran clean against the 5,005-employee seed. |
| `attendance-grid.js` | `GET /api/v1/attendance/grid` | Ran clean against the 5,005-employee seed. |
| `payroll-dry-run.js` | `POST /api/v1/finance/payroll-runs/{id}/dry-run` | See below — this is the one that found real bugs. |

## What the payroll dry-run load test found

`RunDryRunCommandHandler` originally issued up to 4 extra repository round-trips **per employee**
(employee lookup, LOP leave requests, investment declaration, tax regime version) — for a payroll
run covering thousands of employees, thousands of extra queries. This was batched into a fixed
number of queries regardless of headcount, joined in memory afterward (see
`RunDryRunCommandHandler.cs`). The same fix was applied to two dashboard widget providers
(`PendingApprovalsWidgetProvider`, `RealtimeApprovalsCounter`) which had the identical pattern over
proxy delegation chains.

None of this was caught by the existing unit tests, because they mock `IReadRepository<T>` — a
mock never notices that a LINQ expression can't actually be translated to SQL, or that a handler
makes N times as many calls as it should. The N+1 fixes above were verified by asserting call
counts against the mocks (regression guards), but the query-translation bug below was only ever
caught by running the handler against a real Postgres instance under `k6`.

### Real bug: `LeaveRequest.Period` can't be filtered by `Start`/`End` in SQL

`LeaveRequestConfiguration` maps `Period` (a `DateRange` value object) via a single-column
`HasConversion` — the whole object serializes to one `"start|end"` string column, not two real
columns via `OwnsOne` (see that configuration's own comment for why: EF can't bind an
owned-navigation-typed constructor parameter when materializing `LeaveRequest` via its required
private constructor). That means EF Core has no way to translate `Period.Start`/`Period.End`
member access into SQL.

Both `ApprovedLopLeaveRequestsOverlappingPeriodSpecification` (used by `GeneratePayslipCommandHandler`)
and its batch counterpart `ApprovedLopLeaveRequestsOverlappingPeriodForEmployeesSpecification` (used
by `RunDryRunCommandHandler`) had exactly this predicate in their `Criteria`, and both threw
`InvalidOperationException` at runtime — not a compile error, not a slow query, a hard crash — the
moment either handler ran against a tenant with any LOP-flagged leave request. This affected the
**original**, pre-batching handler too; it was never specific to this phase's refactor, just never
exercised against a real database until the load test did.

Fix: both specifications now filter only on the translatable predicates (tenant, employee(s),
status, `LossOfPayDays > 0`) in SQL, and the period-overlap check moved to an in-memory `.Where`
on the (already small, pre-filtered) result — the same "translatable in SQL, exact logic in
memory" pattern already used elsewhere in this codebase (e.g. the `GroupBy`+`First` tolerance in
`ImportEmployeesCommandHandler`).

### Real bug: bulk-imported master data with duplicate names crashes the import

`ImportEmployeesCommandHandler` resolved CSV `DepartmentCode`/`DesignationTitle`/`LocationName`
references via `.ToDictionary(...)`, which throws `ArgumentException` on any duplicate key — and
none of `Department.Code`, `Designation.Title`, or `Location.Name` has a database uniqueness
constraint. Surfaced by the load-testing scripts creating overlapping master data across repeated
runs against the same database. Fixed with `GroupBy(...).ToDictionary(g => g.Key, g => g.First())`,
tolerating duplicates by taking any one match rather than crashing the whole import batch.

### Gap: `GET /api/v1/employees` has no search/filter support

`PagedRequest.SortBy` is accepted by the API surface but never read by `EmployeesPagedSpecification`
(which hardcodes `OrderBy(LastName)` and a tenant+not-deleted `Criteria`) — there is no way to
filter the list by code, name, or anything else. `payroll-dry-run.js` originally relied on a
`search=` query parameter this endpoint silently ignores (ASP.NET drops unknown query params
rather than erroring), which meant every run of the script returned the same page of employees
regardless of what it had just created, causing salary-structure creation to collide with a prior
run's employees. Worked around in the script by creating employees one at a time via
`POST /api/v1/employees` (which returns the new id directly) instead of round-tripping through
bulk import + list. Adding real search/filter support to the employees list endpoint is a
candidate for future work, not done here — it's a product feature gap, not a performance bug.

## Indexes added from real query plans

Verified via `EXPLAIN ANALYZE` against a docker-compose Postgres seeded with 5,005 employees
(command: `docker compose exec db psql -U vespera -d vespera -c "EXPLAIN ANALYZE ..."`).

**`GetAttendanceGridQueryHandler`'s employee-page query**, filtered by `DepartmentId` or
`LocationId` when the caller scopes the grid: before the fix below, both filters fell back to a
full sequential scan of `Employee` (5,004 of 5,005 rows removed by the filter, ~18ms). Added
`(TenantId, DepartmentId)` and `(TenantId, LocationId)` composite indexes
(`AddEmployeeDepartmentLocationIndexes` migration) — same query now uses an index scan and returns
in ~1.5ms. The gap between these two numbers only widens as employee count grows; the seq scan is
O(n), the index scan is effectively O(log n).

**Not changed**: `SalaryStructuresActiveOnDateSpecification` (the payroll dry-run's first query,
fetching every active salary structure for a tenant) still does a sequential scan even with only
`IX_SalaryStructure_TenantId` available — confirmed via `EXPLAIN ANALYZE` this is the right choice
at current data volumes, and structurally this query is expected to return nearly every row for
its tenant (it's "give me every currently-active structure," not a narrow filter), so a composite
index wouldn't meaningfully help even at higher volumes. The batched LOP/investment-declaration/
tax-regime-version queries introduced by this phase's N+1 fix already have adequate covering
indexes (`IX_LeaveRequest_TenantId_EmployeeId`, `IX_InvestmentDeclaration_TenantId_EmployeeId_FinancialYear`,
primary-key `Contains` lookups) — verified by inspection of `\di` output, no new index needed.

## Running EXPLAIN ANALYZE yourself

```
docker compose exec db psql -U vespera -d vespera -c "\di"                    # list all indexes
docker compose exec db psql -U vespera -d vespera -c "EXPLAIN ANALYZE <query>"
```
