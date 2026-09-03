# 0007. Testing depth: tiered coverage gates, payroll-scoped mutation testing

**Status:** Accepted

## Context

Not every layer of the codebase carries the same cost when it's wrong, and line coverage alone
doesn't prove a test suite actually exercises the *logic* it covers — a test can execute every line
of a function and still not assert on any behavior that would catch a subtly wrong calculation.
Payroll specifically is where "subtly wrong" is most expensive: a mis-computed tax slab or
rounding rule doesn't crash, it silently pays someone the wrong amount.

## Decision

Two separate mechanisms, deliberately not applied uniformly. **Coverage gates**
(`.github/workflows/ci.yml`'s `coverage-gate` job) are tiered by layer: Domain (pure business
rules, zero infrastructure dependencies) held to 90% line coverage; Application (CQRS
orchestration — a meaningful fraction of which is thin mapping/DTO code) held to a lower 80%.
**Mutation testing** (Stryker, `tests/Vespera.Application.UnitTests/stryker-config.json`) is not
run repo-wide — it's scoped to exactly `Features/Payroll/PayrollComputationEngine.cs` and
`Features/Payroll/Rules/**/*.cs`, with an 80%/60% high/low threshold and a 60% break threshold,
because that's the one area where "the tests pass but don't actually verify the arithmetic" is the
specific failure mode worth Stryker's cost (mutation testing is CPU-expensive; concurrency is
capped at 4 in CI) to rule out.

## Alternatives considered

- **One uniform coverage gate for every layer.** Rejected: a single high bar (e.g. 90% everywhere)
  either fails Application's legitimately-thinner-value mapping code for no real benefit, or a
  single lower bar (e.g. 80% everywhere) under-protects Domain, where the actual business rules
  live and where near-total coverage is realistically achievable since there's no infrastructure to
  mock around.
- **Repo-wide mutation testing.** Rejected on cost/benefit: Stryker's per-mutant recompile-and-
  rerun cost scales with codebase size; running it over the entire Application layer (not just
  Payroll) would multiply CI time for marginal benefit in areas where a wrong calculation isn't the
  dominant risk the way it is in payroll.
- **No mutation testing at all, coverage gates only.** Rejected specifically for payroll: this
  engagement's own testing-completeness phase was scoped around exactly the risk that high line
  coverage in `PayrollComputationEngine.cs` could still hide an assertion-free test that never
  actually checks the computed amount is *correct*, only that the method didn't throw.

## Consequences

- A contributor touching Payroll rules gets a materially slower CI feedback loop on that specific
  code than anywhere else in the codebase — an intentional trade, not an oversight.
- The 80%/60% Stryker thresholds mean some surviving mutants in payroll rules are tolerated (below
  80% but at/above 60% doesn't fail the build, only warns) — this is a deliberately calibrated
  starting bar, not a claim that every payroll edge case is mutation-proof; the `break: 60`
  threshold is the one that actually fails CI.
- Extending mutation testing to a new area later means updating `stryker-config.json`'s `mutate`
  glob and re-baselining its thresholds against that area's actual mutation score — not something
  that happens automatically as new code is added elsewhere.
