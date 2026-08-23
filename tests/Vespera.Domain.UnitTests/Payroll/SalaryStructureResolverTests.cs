using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class SalaryStructureResolverTests
{
    [Fact]
    public void ResolveMonthly_Should_Compute_Basic_Hra_And_Remainder()
    {
        var basicId = SalaryComponentId.New();
        var hraId = SalaryComponentId.New();
        var specialAllowanceId = SalaryComponentId.New();
        var lines = new[]
        {
            SalaryStructureLine.Of(basicId, SalaryComponentFormula.FixedAmount(Money.Of(40000m, Currency.Inr))),
            SalaryStructureLine.Of(hraId, SalaryComponentFormula.PercentageOfComponent(basicId, 40m)),
            SalaryStructureLine.Of(specialAllowanceId, SalaryComponentFormula.RemainderOfCtc()),
        };
        var structure = SalaryStructure.Create(TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null).Value;

        var result = SalaryStructureResolver.ResolveMonthly(structure, Money.Of(70000m, Currency.Inr));

        result.IsSuccess.Should().BeTrue();
        result.Value[basicId].Should().Be(Money.Of(40000m, Currency.Inr));
        result.Value[hraId].Should().Be(Money.Of(16000m, Currency.Inr), "HRA is 40% of the ₹40,000 Basic");
        result.Value[specialAllowanceId].Should().Be(Money.Of(14000m, Currency.Inr), "70,000 - 40,000 - 16,000 = 14,000");
    }

    [Fact]
    public void ResolveMonthly_Should_Resolve_A_Sum_Of_Components_Line()
    {
        var basicId = SalaryComponentId.New();
        var hraId = SalaryComponentId.New();
        var grossEarningsId = SalaryComponentId.New();
        var lines = new[]
        {
            SalaryStructureLine.Of(basicId, SalaryComponentFormula.FixedAmount(Money.Of(40000m, Currency.Inr))),
            SalaryStructureLine.Of(hraId, SalaryComponentFormula.FixedAmount(Money.Of(16000m, Currency.Inr))),
            SalaryStructureLine.Of(grossEarningsId, SalaryComponentFormula.SumOfComponents([basicId, hraId])),
        };
        var structure = SalaryStructure.Create(TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null).Value;

        var result = SalaryStructureResolver.ResolveMonthly(structure, Money.Of(56000m, Currency.Inr));

        result.IsSuccess.Should().BeTrue();
        result.Value[grossEarningsId].Should().Be(Money.Of(56000m, Currency.Inr));
    }

    [Fact]
    public void ResolveMonthly_Should_Fail_When_Fixed_And_Percentage_Lines_Already_Exceed_The_Target_Ctc()
    {
        var basicId = SalaryComponentId.New();
        var specialAllowanceId = SalaryComponentId.New();
        var lines = new[]
        {
            SalaryStructureLine.Of(basicId, SalaryComponentFormula.FixedAmount(Money.Of(80000m, Currency.Inr))),
            SalaryStructureLine.Of(specialAllowanceId, SalaryComponentFormula.RemainderOfCtc()),
        };
        var structure = SalaryStructure.Create(TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null).Value;

        var result = SalaryStructureResolver.ResolveMonthly(structure, Money.Of(70000m, Currency.Inr));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ResolveMonthly_Should_Resolve_Without_A_Remainder_Line_When_Every_Line_Is_Fixed_Or_Percentage()
    {
        var basicId = SalaryComponentId.New();
        var hraId = SalaryComponentId.New();
        var lines = new[]
        {
            SalaryStructureLine.Of(basicId, SalaryComponentFormula.FixedAmount(Money.Of(40000m, Currency.Inr))),
            SalaryStructureLine.Of(hraId, SalaryComponentFormula.PercentageOfComponent(basicId, 40m)),
        };
        var structure = SalaryStructure.Create(TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null).Value;

        var result = SalaryStructureResolver.ResolveMonthly(structure, Money.Of(56000m, Currency.Inr));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[hraId].Should().Be(Money.Of(16000m, Currency.Inr));
    }
}
