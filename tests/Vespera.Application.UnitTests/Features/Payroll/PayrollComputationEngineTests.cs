using FluentAssertions;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Payroll;
using Vespera.Application.Features.Payroll.Rules;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

public class PayrollComputationEngineTests
{
    private static readonly DateOnly PeriodStart = new(2026, 5, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 5, 31);

    private static PayrollComputationEngine FullPipelineEngine() => new(
    [
        new EarningsRule(), new AttendanceLopRule(), new ProvidentFundRule(), new EmployeeStateInsuranceRule(),
        new ProfessionalTaxRule(), new IncomeTaxRule(), new VoluntaryDeductionsRule(), new ReimbursementsRule(), new NetPayRule(),
    ]);

    [Fact]
    public void ComputeForEmployee_Should_Run_Every_Applicable_Stage_And_Produce_A_Consistent_Rollup()
    {
        var (basicId, hraId) = (SalaryComponentId.New(), SalaryComponentId.New());
        var salaryComponents = new[]
        {
            SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, isTaxable: true, DateTimeOffset.UtcNow, "seed").Value,
            SalaryComponent.Create(TenantId.New(), "HRA", SalaryComponentType.Earning, isTaxable: true, DateTimeOffset.UtcNow, "seed").Value,
        };
        // Line up the synthetic components' Ids with the resolved dictionary, since SalaryComponent.Create
        // always mints its own fresh Id — tests build the dictionary keyed by those real Ids instead.
        var resolved = new Dictionary<SalaryComponentId, Money>
        {
            [salaryComponents[0].Id] = Money.Of(40000m, Currency.Inr),
            [salaryComponents[1].Id] = Money.Of(16000m, Currency.Inr),
        };

        var statutoryRules = new[]
        {
            StatutoryRuleSet.Create(TenantId.New(), StatutoryRuleType.ProvidentFund, 12m, Money.Of(1800m, Currency.Inr), PeriodStart, null).Value,
            StatutoryRuleSet.Create(TenantId.New(), StatutoryRuleType.EmployeeStateInsurance, 0.75m, Money.Of(21000m, Currency.Inr), PeriodStart, null).Value,
            StatutoryRuleSet.Create(TenantId.New(), StatutoryRuleType.ProfessionalTax, 0.2m, Money.Of(200m, Currency.Inr), PeriodStart, null).Value,
        };

        var context = new PayrollContext(
            TenantId.New(), PayrollRunId.New(), EmployeeId.New(), 5, 2026, Currency.Inr,
            PeriodStart, PeriodEnd, new DateOnly(2024, 1, 15), null,
            resolved, salaryComponents, lossOfPayDays: 0m, statutoryRules, taxRegimeVersion: null,
            Money.Zero(Currency.Inr), [], []);

        var result = FullPipelineEngine().ComputeForEmployee(context);

        // Gross = 40000 + 16000 = 56000. Wage base after LOP (none) = 56000.
        // PF = min(12% * 56000, 1800) = min(6720, 1800) = 1800.
        // ESI: 56000 > 21000 ceiling, so it does not apply at all.
        // PT = min(0.2% * 56000, 200) = min(112, 200) = 112.
        result.Gross.Should().Be(Money.Of(56000m, Currency.Inr));
        result.Deductions.Should().Be(Money.Of(1912m, Currency.Inr), "PF 1800 + PT 112, ESI does not apply above its wage ceiling");
        result.Net.Should().Be(Money.Of(54088m, Currency.Inr));
        context.Lines.Should().NotContain(line => line.ComponentId == SyntheticSalaryComponentIds.EmployeeStateInsurance);
    }

    [Fact]
    public void ComputeForEmployee_Should_Apply_Esi_When_Wage_Is_At_Or_Below_The_Ceiling()
    {
        var basicComponent = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        var resolved = new Dictionary<SalaryComponentId, Money> { [basicComponent.Id] = Money.Of(18000m, Currency.Inr) };
        var statutoryRules = new[]
        {
            StatutoryRuleSet.Create(TenantId.New(), StatutoryRuleType.EmployeeStateInsurance, 0.75m, Money.Of(21000m, Currency.Inr), PeriodStart, null).Value,
        };

        var context = new PayrollContext(
            TenantId.New(), PayrollRunId.New(), EmployeeId.New(), 5, 2026, Currency.Inr,
            PeriodStart, PeriodEnd, new DateOnly(2024, 1, 15), null,
            resolved, [basicComponent], 0m, statutoryRules, null, Money.Zero(Currency.Inr), [], []);

        FullPipelineEngine().ComputeForEmployee(context);

        var esiLine = context.Lines.Should().ContainSingle(line => line.ComponentId == SyntheticSalaryComponentIds.EmployeeStateInsurance).Subject;
        esiLine.Amount.Should().Be(Money.Of(135m, Currency.Inr), "0.75% of 18000");
    }

    [Fact]
    public void ComputeForEmployee_Should_Prorate_Earnings_For_A_MidPeriod_Joiner()
    {
        var basicComponent = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        var resolved = new Dictionary<SalaryComponentId, Money> { [basicComponent.Id] = Money.Of(31000m, Currency.Inr) };

        // Joined on the 16th of a 31-day May: 16 days worked out of 31.
        var context = new PayrollContext(
            TenantId.New(), PayrollRunId.New(), EmployeeId.New(), 5, 2026, Currency.Inr,
            PeriodStart, PeriodEnd, new DateOnly(2026, 5, 16), null,
            resolved, [basicComponent], 0m, [], null, Money.Zero(Currency.Inr), [], []);

        var result = FullPipelineEngine().ComputeForEmployee(context);

        result.Gross.Should().Be(Money.Of(16000m, Currency.Inr), "31000 * 16/31 = 16000");
    }

    [Fact]
    public void ComputeForEmployee_Should_Deduct_Loss_Of_Pay_Before_Computing_Statutory_Contributions()
    {
        var basicComponent = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        var resolved = new Dictionary<SalaryComponentId, Money> { [basicComponent.Id] = Money.Of(31000m, Currency.Inr) };
        var statutoryRules = new[]
        {
            StatutoryRuleSet.Create(TenantId.New(), StatutoryRuleType.ProvidentFund, 12m, null, PeriodStart, null).Value,
        };

        // 2 LOP days out of a 31-day period.
        var context = new PayrollContext(
            TenantId.New(), PayrollRunId.New(), EmployeeId.New(), 5, 2026, Currency.Inr,
            PeriodStart, PeriodEnd, new DateOnly(2024, 1, 15), null,
            resolved, [basicComponent], lossOfPayDays: 2m, statutoryRules, null, Money.Zero(Currency.Inr), [], []);

        var result = FullPipelineEngine().ComputeForEmployee(context);

        // Per-day rate = 31000 / 31 = 1000; LOP = 2000. Wage base for PF = 31000 - 2000 = 29000; PF = 12% = 3480.
        var lopLine = context.Lines.Should().ContainSingle(line => line.ComponentId == SyntheticSalaryComponentIds.LossOfPay).Subject;
        lopLine.Amount.Should().Be(Money.Of(2000m, Currency.Inr));
        var pfLine = context.Lines.Should().ContainSingle(line => line.ComponentId == SyntheticSalaryComponentIds.ProvidentFund).Subject;
        pfLine.Amount.Should().Be(Money.Of(3480m, Currency.Inr));
        result.Net.Should().Be(Money.Of(31000m - 2000m - 3480m, Currency.Inr));
    }

    [Fact]
    public void ComputeForEmployee_Should_Skip_Income_Tax_When_No_Regime_Is_Resolved()
    {
        var basicComponent = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        var resolved = new Dictionary<SalaryComponentId, Money> { [basicComponent.Id] = Money.Of(200000m, Currency.Inr) };

        var context = new PayrollContext(
            TenantId.New(), PayrollRunId.New(), EmployeeId.New(), 5, 2026, Currency.Inr,
            PeriodStart, PeriodEnd, new DateOnly(2024, 1, 15), null,
            resolved, [basicComponent], 0m, [], taxRegimeVersion: null, Money.Zero(Currency.Inr), [], []);

        FullPipelineEngine().ComputeForEmployee(context);

        context.Lines.Should().NotContain(line => line.ComponentId == SyntheticSalaryComponentIds.IncomeTax);
    }

    [Fact]
    public void ComputeForEmployee_Should_Compute_Monthly_Tds_From_The_Annualized_Regime_Calculation()
    {
        var basicComponent = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        var resolved = new Dictionary<SalaryComponentId, Money> { [basicComponent.Id] = Money.Of(100000m, Currency.Inr) };
        var regime = TaxRegimeVersion.Create(
            TenantId.New(), TaxRegimeType.New, "2026-27",
            [
                TaxSlab.Create(Money.Of(1200000m, Currency.Inr), 0m).Value,
                TaxSlab.Create(Money.Of(decimal.MaxValue / 2, Currency.Inr), 20m).Value,
            ]).Value;

        var context = new PayrollContext(
            TenantId.New(), PayrollRunId.New(), EmployeeId.New(), 5, 2026, Currency.Inr,
            PeriodStart, PeriodEnd, new DateOnly(2024, 1, 15), null,
            resolved, [basicComponent], 0m, [], regime, Money.Zero(Currency.Inr), [], []);

        FullPipelineEngine().ComputeForEmployee(context);

        // Annual income = 100000 * 12 = 1,200,000, exactly at the 0% slab ceiling -> zero tax, no line emitted.
        context.Lines.Should().NotContain(line => line.ComponentId == SyntheticSalaryComponentIds.IncomeTax);
    }

    [Fact]
    public void ComputeForEmployee_Should_Include_Voluntary_Deductions_And_Reimbursements()
    {
        var basicComponent = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        var resolved = new Dictionary<SalaryComponentId, Money> { [basicComponent.Id] = Money.Of(50000m, Currency.Inr) };
        var loanEmi = new PayrollAdHocLine(SalaryComponentId.New(), "Loan EMI", Money.Of(5000m, Currency.Inr));
        var travelReimbursement = new PayrollAdHocLine(SalaryComponentId.New(), "Travel Reimbursement", Money.Of(2000m, Currency.Inr));

        var context = new PayrollContext(
            TenantId.New(), PayrollRunId.New(), EmployeeId.New(), 5, 2026, Currency.Inr,
            PeriodStart, PeriodEnd, new DateOnly(2024, 1, 15), null,
            resolved, [basicComponent], 0m, [], null, Money.Zero(Currency.Inr), [loanEmi], [travelReimbursement]);

        var result = FullPipelineEngine().ComputeForEmployee(context);

        result.Gross.Should().Be(Money.Of(52000m, Currency.Inr), "50000 salary + 2000 reimbursement");
        result.Deductions.Should().Be(Money.Of(5000m, Currency.Inr));
        result.Net.Should().Be(Money.Of(47000m, Currency.Inr));
    }

    [Fact]
    public void ComputeForEmployee_Should_Produce_Identical_Results_For_Two_Runs_On_The_Same_Frozen_Inputs()
    {
        var basicComponent = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        var resolved = new Dictionary<SalaryComponentId, Money> { [basicComponent.Id] = Money.Of(75000m, Currency.Inr) };
        var statutoryRules = new[]
        {
            StatutoryRuleSet.Create(TenantId.New(), StatutoryRuleType.ProvidentFund, 12m, Money.Of(1800m, Currency.Inr), PeriodStart, null).Value,
        };
        var employeeId = EmployeeId.New();

        PayrollContext BuildContext() => new(
            TenantId.New(), PayrollRunId.New(), employeeId, 5, 2026, Currency.Inr,
            PeriodStart, PeriodEnd, new DateOnly(2024, 1, 15), null,
            resolved, [basicComponent], 1.5m, statutoryRules, null, Money.Zero(Currency.Inr), [], []);

        var firstRun = FullPipelineEngine().ComputeForEmployee(BuildContext());
        var secondRun = FullPipelineEngine().ComputeForEmployee(BuildContext());

        firstRun.Should().Be(secondRun, "two dry-runs on the same frozen inputs must be byte-identical");
    }
}
