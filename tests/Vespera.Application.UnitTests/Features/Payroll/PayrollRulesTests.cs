using FluentAssertions;
using Vespera.Application.Features.Payroll.Rules;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.UnitTests.Features.Payroll;

/// <summary>
/// Direct, isolated tests for individual <see cref="IPayrollComponentRule"/> stages — narrower than
/// <see cref="PayrollComputationEngineTests"/>'s end-to-end pipeline scenarios, written to close
/// specific gaps a Stryker mutation-testing run against Features/Payroll/Rules found (see
/// docs/testing.md): boundary/gating conditions the end-to-end tests never happened to exercise.
/// </summary>
public class PayrollRulesTests
{
    private static readonly DateOnly PeriodStart = new(2026, 5, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 5, 31);

    private static PayrollContext BuildContext(
        IReadOnlyDictionary<SalaryComponentId, Money>? resolved = null,
        IReadOnlyList<SalaryComponent>? salaryComponents = null,
        decimal lossOfPayDays = 0m,
        IReadOnlyList<StatutoryRuleSet>? statutoryRules = null,
        TaxRegimeVersion? taxRegimeVersion = null,
        Money? approvedInvestmentExemptions = null,
        IReadOnlyList<PayrollAdHocLine>? voluntaryDeductions = null,
        IReadOnlyList<PayrollAdHocLine>? reimbursements = null) => new(
        TenantId.New(), PayrollRunId.New(), EmployeeId.New(), 5, 2026, Currency.Inr,
        PeriodStart, PeriodEnd, new DateOnly(2024, 1, 15), null,
        resolved ?? new Dictionary<SalaryComponentId, Money>(), salaryComponents ?? [],
        lossOfPayDays, statutoryRules ?? [], taxRegimeVersion,
        approvedInvestmentExemptions ?? Money.Zero(Currency.Inr), voluntaryDeductions ?? [], reimbursements ?? []);

    [Fact]
    public void EarningsRule_AppliesTo_Should_Be_False_When_There_Are_No_Resolved_Components()
    {
        var context = BuildContext();

        new EarningsRule().AppliesTo(context).Should().BeFalse();
    }

    [Fact]
    public void EarningsRule_Apply_Should_Skip_A_Resolved_Component_With_No_Matching_SalaryComponent()
    {
        // The resolved-amounts dictionary is keyed by a component Id that isn't present in
        // context.SalaryComponents — this is the case FirstOrDefault (not First) exists to handle.
        var unmatchedId = SalaryComponentId.New();
        var context = BuildContext(
            resolved: new Dictionary<SalaryComponentId, Money> { [unmatchedId] = Money.Of(1000m, Currency.Inr) },
            salaryComponents: []);

        var act = () => new EarningsRule().Apply(context);

        act.Should().NotThrow();
        act().Should().BeEmpty();
    }

    [Fact]
    public void EmployeeStateInsuranceRule_AppliesTo_Should_Be_True_When_Wage_Is_Exactly_At_The_Ceiling()
    {
        var basic = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        var resolved = new Dictionary<SalaryComponentId, Money> { [basic.Id] = Money.Of(21000m, Currency.Inr) };
        var rule = StatutoryRuleSet.Create(TenantId.New(), StatutoryRuleType.EmployeeStateInsurance, 0.75m, Money.Of(21000m, Currency.Inr), PeriodStart, null).Value;
        var context = BuildContext(resolved, [basic], statutoryRules: [rule]);
        // PayrollWageBase reads its gross from context.Lines — without this, wage base would read as
        // zero (trivially "at or below" any positive cap) and the boundary wouldn't be exercised at all.
        context.AppendLines(new EarningsRule().Apply(context));

        new EmployeeStateInsuranceRule().AppliesTo(context).Should().BeTrue("the wage ceiling is inclusive — exactly at the cap still qualifies for ESI");
    }

    [Fact]
    public void EmployeeStateInsuranceRule_Apply_Should_Label_The_Line_Employee_State_Insurance()
    {
        var basic = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        var resolved = new Dictionary<SalaryComponentId, Money> { [basic.Id] = Money.Of(18000m, Currency.Inr) };
        var rule = StatutoryRuleSet.Create(TenantId.New(), StatutoryRuleType.EmployeeStateInsurance, 0.75m, Money.Of(21000m, Currency.Inr), PeriodStart, null).Value;
        var context = BuildContext(resolved, [basic], statutoryRules: [rule]);

        var line = new EmployeeStateInsuranceRule().Apply(context).Single();

        line.ComponentName.Should().Be("Employee State Insurance");
    }

    [Fact]
    public void IncomeTaxRule_Apply_Should_Subtract_Approved_Investment_Exemptions_Before_Calculating_Tax()
    {
        var basic = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        // Annualized income = 100000 * 12 = 1,200,000 (exactly at the 0%-slab ceiling on its own).
        var resolved = new Dictionary<SalaryComponentId, Money> { [basic.Id] = Money.Of(100000m, Currency.Inr) };
        var regime = TaxRegimeVersion.Create(
            TenantId.New(), TaxRegimeType.New, "2026-27",
            [
                TaxSlab.Create(Money.Of(1000000m, Currency.Inr), 0m).Value,
                TaxSlab.Create(Money.Of(decimal.MaxValue / 2, Currency.Inr), 20m).Value,
            ]).Value;

        // Without exemptions, taxable income (1,200,000) exceeds the 0% ceiling (1,000,000) and 20%
        // tax applies to the remainder. Subtracting a 200,000 exemption brings taxable income back
        // down to exactly the ceiling, so tax should drop to zero — the opposite of what an
        // addition-instead-of-subtraction mutant would produce. PayrollWageBase reads its gross
        // from context.Lines, so EarningsRule must run first, exactly as the real pipeline order.
        var withoutExemption = BuildContext(resolved, [basic], taxRegimeVersion: regime);
        withoutExemption.AppendLines(new EarningsRule().Apply(withoutExemption));
        var withExemption = BuildContext(resolved, [basic], taxRegimeVersion: regime, approvedInvestmentExemptions: Money.Of(200000m, Currency.Inr));
        withExemption.AppendLines(new EarningsRule().Apply(withExemption));

        var rule = new IncomeTaxRule();
        rule.Apply(withoutExemption).Should().ContainSingle(line => line.ComponentId == SyntheticSalaryComponentIds.IncomeTax);
        rule.Apply(withExemption).Should().BeEmpty("the exemption should reduce taxable income to the zero-tax ceiling, not push it further above");
    }

    [Fact]
    public void NetPayRule_AppliesTo_Should_Always_Be_True()
    {
        new NetPayRule().AppliesTo(BuildContext()).Should().BeTrue();
    }

    [Fact]
    public void ReimbursementsRule_AppliesTo_Should_Be_False_When_There_Are_No_Reimbursements()
    {
        new ReimbursementsRule().AppliesTo(BuildContext()).Should().BeFalse();
    }

    [Fact]
    public void VoluntaryDeductionsRule_AppliesTo_Should_Be_False_When_There_Are_No_Voluntary_Deductions()
    {
        new VoluntaryDeductionsRule().AppliesTo(BuildContext()).Should().BeFalse();
    }

    [Fact]
    public void ProfessionalTaxRule_Apply_Should_Clamp_The_Contribution_To_The_Cap_When_It_Would_Exceed_It()
    {
        var basic = SalaryComponent.Create(TenantId.New(), "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value;
        // 0.2% of 200,000 = 400, which is well above the 200 cap.
        var resolved = new Dictionary<SalaryComponentId, Money> { [basic.Id] = Money.Of(200000m, Currency.Inr) };
        var rule = StatutoryRuleSet.Create(TenantId.New(), StatutoryRuleType.ProfessionalTax, 0.2m, Money.Of(200m, Currency.Inr), PeriodStart, null).Value;
        var context = BuildContext(resolved, [basic], statutoryRules: [rule]);
        context.AppendLines(new EarningsRule().Apply(context));

        var line = new ProfessionalTaxRule().Apply(context).Single();

        line.Amount.Should().Be(Money.Of(200m, Currency.Inr), "the computed 400 must be clamped down to the 200 cap");
    }
}
