# Testing

## Coverage gates

CI enforces a line-coverage floor on the two layers where it matters most:

| Layer | Gate | Rationale |
|---|---|---|
| `Vespera.Domain` | ≥ 90% | Pure business rules, zero infrastructure dependencies — held to a high bar since there's no excuse for an untested branch. |
| `Vespera.Application` | ≥ 80% | CQRS handlers/orchestration — held slightly lower since a portion of it is thin mapping/DTO code. |

The `coverage-gate` job in `.github/workflows/ci.yml` re-runs `tests/Vespera.Domain.UnitTests` and
`tests/Vespera.Application.UnitTests` with coverlet's `XPlat Code Coverage` collector (the only
coverage package these test `.csproj` files reference — see `docs/CONTRIBUTING-slices.md`) and
reads the resulting Cobertura XML directly rather than trusting a blended top-level number:

- For Domain, the top-level `<coverage line-rate="...">` is accurate, since that test project only
  references `Vespera.Domain`.
- For Application, the top-level figure is **not** accurate: the Application test run also loads
  `Vespera.Domain` (handlers construct Domain types), so coverlet's instrumentation covers both
  assemblies and the top-level number blends them. The gate instead reads the
  `<package name="Vespera.Application" line-rate="...">` node specifically, which is the real,
  undiluted Application-only figure.

To reproduce locally:

```bash
dotnet test tests/Vespera.Domain.UnitTests/Vespera.Domain.UnitTests.csproj \
  -c Release --collect:"XPlat Code Coverage" --results-directory TestResults/domain-coverage

dotnet test tests/Vespera.Application.UnitTests/Vespera.Application.UnitTests.csproj \
  -c Release --collect:"XPlat Code Coverage" --results-directory TestResults/application-coverage
```

Then open the generated `coverage.cobertura.xml` (or feed it to a report generator like
[ReportGenerator](https://github.com/danielpalme/ReportGenerator) for an HTML view).

## Mutation testing (payroll rules pipeline)

[Stryker.NET](https://stryker-mutator.io/docs/stryker-net/introduction/) is set up as a local
`dotnet tool` (see `.config/dotnet-tools.json`) and scoped specifically to the payroll rules
pipeline — `Vespera.Application/Features/Payroll/PayrollComputationEngine.cs` and everything under
`Features/Payroll/Rules/` — rather than the whole codebase. This is the part of the system where a
silently-wrong mutation (an off-by-one on a statutory cap, a flipped comparison on a wage ceiling)
translates directly into a wrong paycheck, so it's the one place line coverage alone isn't a strong
enough signal: a test can execute every line of `ProfessionalTaxRule.Apply` without ever asserting
the cap actually clamps.

Config lives at `tests/Vespera.Application.UnitTests/stryker-config.json`. To run it:

```bash
cd tests/Vespera.Application.UnitTests
dotnet tool restore
dotnet stryker --config-file stryker-config.json
```

This takes roughly 1–2 minutes (it's deliberately scoped tight — a full-solution mutation run would
take much longer and isn't wired into CI; run it manually when touching the payroll pipeline). It
produces an HTML report under `StrykerOutput/<timestamp>/reports/mutation-report.html`.

As of this setup, the mutation score is **~90%** against a `break` threshold of 60% and `high`
threshold of 80% (see `thresholds` in the config). The remaining survived mutants are all confirmed
**equivalent mutants** — the mutation produces byte-identical output to the original for every
valid input, so no test could ever kill them without becoming a tautology:

- `EarningsRule`'s period-boundary ternaries (`DateOfJoining > PeriodStart ? ... : ...` mutated to
  `>=`, and the symmetric `ExitDate < PeriodEnd`): at the exact boundary, both ternary branches
  evaluate to the same date, so the mutation is unobservable.
- `EarningsRule`'s full-period proration conditional: when `isFullPeriod` is true,
  `effectiveDays == context.DaysInPeriod` always, so `amount * effectiveDays / DaysInPeriod`
  collapses to `amount` exactly — the "always prorate" mutant produces the same number.
- `IncomeTaxRule`'s `taxableIncome.Amount < 0` vs `<= 0`: at exactly zero, resetting to
  `Money.Zero(currency)` is value-equal to the already-zero `taxableIncome` — no observable change.
- `PayrollWageBase`'s `LossOfPayDays <= 0` vs `< 0` (and the paired block-removal mutant): at
  `LossOfPayDays == 0`, the LOP deduction computes to exactly zero either way.
- `ProfessionalTaxRule` and `ProvidentFundRule`'s cap-clamp `contribution > CapAmount` vs `>=`: at
  `contribution == CapAmount` exactly, clamping to `CapAmount` is a no-op — clamped or not, the
  value is identical.

One tooling note: Stryker's per-mutant recompilation loses type inference on
`rules.OrderBy(rule => rule.Order)` inside `PayrollComputationEngine`'s constructor when the
surrounding syntax is a target-typed collection expression or plain `.ToList()`. Pinning explicit
type arguments (`OrderBy<IPayrollComponentRule, int>(...)`) resolves it; without that, every mutant
in the class would silently report as a compile error and get excluded from the run rather than
tested. The same underlying issue still affects a few `OrderBy` call sites outside the payroll
pipeline's mutate scope (`ExpensePolicyEvaluator`, `TicketRoutingEvaluator`,
`StageTransitionEvaluator`) — worth the same fix if their mutation coverage is ever measured.
