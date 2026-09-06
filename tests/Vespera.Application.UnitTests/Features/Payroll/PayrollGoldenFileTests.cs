using System.Text.Json;
using FluentAssertions;
using Vespera.Application.Features.Payroll;
using Vespera.Application.Features.Payroll.Rules;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

/// <summary>
/// Regression gates: each scenario runs the real 9-stage pipeline against a fixed, deterministic
/// input (every id below is a literal Guid, never <c>.New()</c> — required for the serialized
/// output to be comparable across runs at all) and diffs the result against a checked-in JSON
/// snapshot in <c>GoldenFiles/</c>. A snapshot mismatch means the pipeline's output changed for a
/// scenario nobody touched — exactly the class of regression a payroll engine cannot ship with
/// unnoticed. If a golden file is missing (first run, or a deliberately new scenario), the test
/// writes it and fails with an explicit message asking for a second run and a manual review of the
/// generated file — the same bootstrap idiom snapshot-testing tools use elsewhere.
/// </summary>
public class PayrollGoldenFileTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly TenantId TenantId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly EmployeeId EmployeeId = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly PayrollRunId PayrollRunId = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));

    // SalaryComponent.Create always mints its own fresh Id, so these are built once and referenced
    // by .Id everywhere below — the golden-file snapshot never serializes ComponentId itself (only
    // ComponentName/Direction/Amount), so the Id only needs to be *consistent within one run*, not
    // a literal constant.
    private static readonly SalaryComponent BasicComponent =
        SalaryComponent.Create(TenantId, "Basic", SalaryComponentType.Earning, isTaxable: true, DateTimeOffset.UnixEpoch, "golden-file-fixture").Value;

    private static readonly SalaryComponent HraComponent =
        SalaryComponent.Create(TenantId, "HRA", SalaryComponentType.Earning, isTaxable: true, DateTimeOffset.UnixEpoch, "golden-file-fixture").Value;

    private static readonly SalaryComponent[] SalaryComponents = [BasicComponent, HraComponent];

    private static readonly StatutoryRuleSet[] StatutoryRuleSets =
    [
        StatutoryRuleSet.Create(TenantId, StatutoryRuleType.ProvidentFund, 12m, Money.Of(1800m, Currency.Inr), new DateOnly(2026, 4, 1), null).Value,
        StatutoryRuleSet.Create(TenantId, StatutoryRuleType.EmployeeStateInsurance, 0.75m, Money.Of(21000m, Currency.Inr), new DateOnly(2026, 4, 1), null).Value,
        StatutoryRuleSet.Create(TenantId, StatutoryRuleType.ProfessionalTax, 0.2m, Money.Of(200m, Currency.Inr), new DateOnly(2026, 4, 1), null).Value,
    ];

    private static PayrollComputationEngine FullPipelineEngine() => new(
    [
        new EarningsRule(), new AttendanceLopRule(), new ProvidentFundRule(), new EmployeeStateInsuranceRule(),
        new ProfessionalTaxRule(), new IncomeTaxRule(), new VoluntaryDeductionsRule(), new ReimbursementsRule(), new NetPayRule(),
    ]);

    [Fact]
    public void OldRegimeEmployee() => RunScenario("old-regime-employee", month: 5, dateOfJoining: new DateOnly(2020, 1, 1), exitDate: null, lossOfPayDays: 0m, regime: OldRegime());

    [Fact]
    public void NewRegimeEmployee() => RunScenario("new-regime-employee", month: 5, dateOfJoining: new DateOnly(2020, 1, 1), exitDate: null, lossOfPayDays: 0m, regime: NewRegime());

    [Fact]
    public void MidMonthJoiner() => RunScenario("mid-month-joiner", month: 5, dateOfJoining: new DateOnly(2026, 5, 16), exitDate: null, lossOfPayDays: 0m, regime: null);

    [Fact]
    public void MidMonthExit() => RunScenario("mid-month-exit", month: 5, dateOfJoining: new DateOnly(2020, 1, 1), exitDate: new DateOnly(2026, 5, 15), lossOfPayDays: 0m, regime: null);

    [Fact]
    public void LossOfPay() => RunScenario("loss-of-pay", month: 5, dateOfJoining: new DateOnly(2020, 1, 1), exitDate: null, lossOfPayDays: 3m, regime: null);

    [Fact]
    public void FullYearRollup()
    {
        var monthlyNets = new List<decimal>();
        for (var month = 1; month <= 12; month++)
        {
            var year = month >= 4 ? 2026 : 2027;
            var periodStart = new DateOnly(year, month, 1);
            var periodEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            var context = BuildContext(month, year, periodStart, periodEnd, new DateOnly(2020, 1, 1), null, 0m, OldRegime());
            var result = FullPipelineEngine().ComputeForEmployee(context);
            monthlyNets.Add(result.Net.Amount);
        }

        AssertMatchesGoldenFile("full-year-rollup", new { MonthlyNets = monthlyNets, AnnualTotal = monthlyNets.Sum() });
    }

    private static void RunScenario(string scenarioName, int month, DateOnly dateOfJoining, DateOnly? exitDate, decimal lossOfPayDays, TaxRegimeVersion? regime)
    {
        var periodStart = new DateOnly(2026, month, 1);
        var periodEnd = new DateOnly(2026, month, DateTime.DaysInMonth(2026, month));
        var context = BuildContext(month, 2026, periodStart, periodEnd, dateOfJoining, exitDate, lossOfPayDays, regime);

        var result = FullPipelineEngine().ComputeForEmployee(context);

        var snapshot = new
        {
            Gross = result.Gross.Amount,
            Deductions = result.Deductions.Amount,
            Net = result.Net.Amount,
            result.LossOfPayDays,
            Lines = context.Lines
                .Select(line => new { line.ComponentName, Direction = line.Direction.ToString(), Amount = line.Amount.Amount })
                .OrderBy(line => line.ComponentName, StringComparer.Ordinal)
                .ToList(),
        };

        AssertMatchesGoldenFile(scenarioName, snapshot);
    }

    private static PayrollContext BuildContext(
        int month, int year, DateOnly periodStart, DateOnly periodEnd, DateOnly dateOfJoining, DateOnly? exitDate,
        decimal lossOfPayDays, TaxRegimeVersion? regime)
    {
        var resolved = new Dictionary<SalaryComponentId, Money>
        {
            [BasicComponent.Id] = Money.Of(50000m, Currency.Inr),
            [HraComponent.Id] = Money.Of(20000m, Currency.Inr),
        };

        return new PayrollContext(
            TenantId, PayrollRunId, EmployeeId, month, year, Currency.Inr, periodStart, periodEnd, dateOfJoining, exitDate,
            resolved, SalaryComponents, lossOfPayDays, StatutoryRuleSets, regime, Money.Zero(Currency.Inr), [], []);
    }

    private static TaxRegimeVersion OldRegime() => TaxRegimeVersion.Create(
        TenantId, TaxRegimeType.Old, "2026-27",
        [
            TaxSlab.Create(Money.Of(250000m, Currency.Inr), 0m).Value,
            TaxSlab.Create(Money.Of(500000m, Currency.Inr), 5m).Value,
            TaxSlab.Create(Money.Of(1000000m, Currency.Inr), 20m).Value,
            TaxSlab.Create(Money.Of(100000000m, Currency.Inr), 30m).Value,
        ]).Value;

    private static TaxRegimeVersion NewRegime() => TaxRegimeVersion.Create(
        TenantId, TaxRegimeType.New, "2026-27",
        [
            TaxSlab.Create(Money.Of(700000m, Currency.Inr), 0m).Value,
            TaxSlab.Create(Money.Of(1000000m, Currency.Inr), 10m).Value,
            TaxSlab.Create(Money.Of(1200000m, Currency.Inr), 15m).Value,
            TaxSlab.Create(Money.Of(100000000m, Currency.Inr), 20m).Value,
        ]).Value;

    private static void AssertMatchesGoldenFile(string scenarioName, object actual)
    {
        var path = GoldenFilePath(scenarioName);
        var actualJson = JsonSerializer.Serialize(actual, JsonOptions);

        if (!File.Exists(path))
        {
            File.WriteAllText(path, actualJson);
            Assert.Fail($"Golden file '{scenarioName}.json' did not exist and was created at {path}. Review it and re-run the test.");
        }

        var expectedJson = File.ReadAllText(path);

        // JsonSerializer's indented writer uses Environment.NewLine for its indentation breaks,
        // so the same serialization comes out CRLF on Windows and LF elsewhere — normalize both
        // sides before comparing so this asserts on the actual JSON content, not on which OS
        // happened to check the golden file in.
        actualJson.Replace("\r\n", "\n").Should().Be(
            expectedJson.Replace("\r\n", "\n"), $"the pipeline's output for scenario '{scenarioName}' must match its checked-in golden file");
    }

    // [CallerFilePath] is a compile-time constant, which the .NET SDK rewrites to a deterministic
    // "/_/..." placeholder instead of the real checkout path whenever ContinuousIntegrationBuild
    // is enabled (Directory.Build.props turns that on whenever CI=true, which every CI run sets) —
    // so it must not be used to locate real files at runtime. Walking up from the actual runtime
    // output directory to the repo root (marked by Vespera.sln) works in every environment.
    private static string GoldenFilePath(string scenarioName) =>
        Path.Combine(RepositoryRoot.Value, "tests", "Vespera.Application.UnitTests", "Features", "Payroll", "GoldenFiles", $"{scenarioName}.json");

    private static readonly Lazy<string> RepositoryRoot = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Vespera.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException($"Could not locate Vespera.sln above {AppContext.BaseDirectory}.");
    });
}
