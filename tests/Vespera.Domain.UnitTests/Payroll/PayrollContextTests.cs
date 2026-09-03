using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class PayrollContextTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly PayrollRunId PayrollRunId = PayrollRunId.New();
    private static readonly EmployeeId EmployeeId = EmployeeId.New();
    private static readonly DateOnly PeriodStart = new(2026, 5, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 5, 31);

    private static PayrollContext CreateContext(
        IReadOnlyList<StatutoryRuleSet>? statutoryRuleSets = null, IReadOnlyList<PayrollAdHocLine>? voluntaryDeductions = null,
        IReadOnlyList<PayrollAdHocLine>? reimbursements = null) =>
        new(
            TenantId, PayrollRunId, EmployeeId, 5, 2026, Currency.Inr, PeriodStart, PeriodEnd, new DateOnly(2024, 1, 15), null,
            new Dictionary<SalaryComponentId, Money>(), [], 0m, statutoryRuleSets ?? [], null, Money.Zero(Currency.Inr),
            voluntaryDeductions ?? [], reimbursements ?? []);

    [Fact]
    public void DaysInPeriod_Should_Count_Inclusively_From_PeriodStart_To_PeriodEnd()
    {
        var context = CreateContext();

        context.DaysInPeriod.Should().Be(31);
    }

    [Fact]
    public void AppendLines_Then_SumEarnings_And_SumDeductions_Should_Total_Only_Their_Own_Direction()
    {
        var context = CreateContext();
        var basicComponentId = SalaryComponentId.New();
        var pfComponentId = SalaryComponentId.New();

        context.AppendLines([
            new PayrollComponentLine(basicComponentId, "Basic", SalaryComponentType.Earning, PayrollComponentDirection.Earning, Money.Of(40000m, Currency.Inr)),
            new PayrollComponentLine(pfComponentId, "PF", SalaryComponentType.StatutoryContribution, PayrollComponentDirection.Deduction, Money.Of(1800m, Currency.Inr)),
        ]);

        context.SumEarnings().Amount.Should().Be(40000m);
        context.SumDeductions().Amount.Should().Be(1800m);
        context.FindAmount(basicComponentId)!.Amount.Should().Be(40000m);
        context.FindAmount(SalaryComponentId.New()).Should().BeNull();
    }

    [Fact]
    public void FindStatutoryRule_Should_Return_The_Rule_Matching_The_Requested_Type()
    {
        var pfRule = StatutoryRuleSet.Create(TenantId, StatutoryRuleType.ProvidentFund, 12m, null, PeriodStart, null).Value;
        var esiRule = StatutoryRuleSet.Create(TenantId, StatutoryRuleType.EmployeeStateInsurance, 3.25m, null, PeriodStart, null).Value;
        var context = CreateContext([pfRule, esiRule]);

        context.FindStatutoryRule(StatutoryRuleType.ProvidentFund).Should().Be(pfRule);
        context.FindStatutoryRule(StatutoryRuleType.ProfessionalTax).Should().BeNull();
    }

    [Fact]
    public void VoluntaryDeductions_And_Reimbursements_Should_Be_Exposed_As_Given()
    {
        var componentId = SalaryComponentId.New();
        var deduction = new PayrollAdHocLine(componentId, "NPS", Money.Of(2000m, Currency.Inr));
        var reimbursement = new PayrollAdHocLine(componentId, "Travel", Money.Of(1500m, Currency.Inr));

        var context = CreateContext(voluntaryDeductions: [deduction], reimbursements: [reimbursement]);

        context.VoluntaryDeductions.Should().ContainSingle().Which.Should().Be(deduction);
        context.Reimbursements.Should().ContainSingle().Which.Should().Be(reimbursement);
    }

    [Fact]
    public void PayrollAdHocLine_Equality_Should_Compare_By_Value()
    {
        var componentId = SalaryComponentId.New();
        var first = new PayrollAdHocLine(componentId, "NPS", Money.Of(2000m, Currency.Inr));
        var second = new PayrollAdHocLine(componentId, "NPS", Money.Of(2000m, Currency.Inr));

        first.Should().Be(second);
    }

    [Fact]
    public void SumEarnings_And_SumDeductions_Should_Be_Zero_When_No_Lines_Were_Appended()
    {
        var context = CreateContext();

        context.SumEarnings().Amount.Should().Be(0m);
        context.SumDeductions().Amount.Should().Be(0m);
        context.Lines.Should().BeEmpty();
    }

    [Fact]
    public void FindStatutoryRule_Should_Return_Null_When_There_Are_No_Statutory_Rule_Sets()
    {
        var context = CreateContext();

        context.FindStatutoryRule(StatutoryRuleType.ProvidentFund).Should().BeNull();
    }

    [Fact]
    public void Constructor_Should_Expose_Every_Field_Including_Optional_ExitDate_And_TaxRegimeVersion()
    {
        var employeeId = EmployeeId.New();
        var resolved = new Dictionary<SalaryComponentId, Money> { [SalaryComponentId.New()] = Money.Of(1000m, Currency.Inr) };
        var salaryComponents = new List<SalaryComponent> { SalaryComponent.Create(TenantId, "Basic", SalaryComponentType.Earning, true, DateTimeOffset.UtcNow, "seed").Value };
        var exitDate = new DateOnly(2026, 5, 20);
        var slabs = new[] { TaxSlab.Create(Money.Of(250_000m, Currency.Inr), 0m).Value };
        var taxRegimeVersion = TaxRegimeVersion.Create(TenantId, TaxRegimeType.New, "2026-27", slabs).Value;
        var exemptions = Money.Of(50000m, Currency.Inr);

        var context = new PayrollContext(
            TenantId, PayrollRunId, employeeId, 5, 2026, Currency.Inr, PeriodStart, PeriodEnd, new DateOnly(2020, 1, 1), exitDate,
            resolved, salaryComponents, 1.5m, [], taxRegimeVersion, exemptions, [], []);

        context.EmployeeId.Should().Be(employeeId);
        context.Month.Should().Be(5);
        context.Year.Should().Be(2026);
        context.Currency.Should().Be(Currency.Inr);
        context.DateOfJoining.Should().Be(new DateOnly(2020, 1, 1));
        context.ExitDate.Should().Be(exitDate);
        context.ResolvedSalaryComponents.Should().BeSameAs(resolved);
        context.SalaryComponents.Should().BeSameAs(salaryComponents);
        context.LossOfPayDays.Should().Be(1.5m);
        context.TaxRegimeVersion.Should().Be(taxRegimeVersion);
        context.ApprovedInvestmentExemptions.Should().Be(exemptions);
    }
}
