using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Payroll;

public class SalaryStructureTests
{
    [Fact]
    public void Create_Should_Fail_With_No_Lines()
    {
        var result = SalaryStructure.Create(
            TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), [], new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Succeed_With_Fixed_And_Percentage_Lines()
    {
        var basicId = SalaryComponentId.New();
        var lines = new[]
        {
            SalaryStructureLine.Of(basicId, SalaryComponentFormula.FixedAmount(Money.Of(50000m, Currency.Inr))),
            SalaryStructureLine.Of(SalaryComponentId.New(), SalaryComponentFormula.PercentageOfComponent(basicId, 40m)),
        };

        var result = SalaryStructure.Create(TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Fail_When_A_Formula_References_A_Component_Not_In_The_Structure()
    {
        var lines = new[]
        {
            SalaryStructureLine.Of(SalaryComponentId.New(), SalaryComponentFormula.PercentageOfComponent(SalaryComponentId.New(), 40m)),
        };

        var result = SalaryStructure.Create(TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Fail_When_Two_Formulas_Reference_Each_Other()
    {
        var componentAId = SalaryComponentId.New();
        var componentBId = SalaryComponentId.New();
        var lines = new[]
        {
            SalaryStructureLine.Of(componentAId, SalaryComponentFormula.PercentageOfComponent(componentBId, 50m)),
            SalaryStructureLine.Of(componentBId, SalaryComponentFormula.PercentageOfComponent(componentAId, 50m)),
        };

        var result = SalaryStructure.Create(TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue("A = 50% of B and B = 50% of A can never converge");
    }

    [Fact]
    public void Create_Should_Fail_With_Two_Remainder_Of_Ctc_Lines()
    {
        var lines = new[]
        {
            SalaryStructureLine.Of(SalaryComponentId.New(), SalaryComponentFormula.RemainderOfCtc()),
            SalaryStructureLine.Of(SalaryComponentId.New(), SalaryComponentFormula.RemainderOfCtc()),
        };

        var result = SalaryStructure.Create(TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Fail_With_A_Duplicate_Component()
    {
        var componentId = SalaryComponentId.New();
        var lines = new[]
        {
            SalaryStructureLine.Of(componentId, SalaryComponentFormula.FixedAmount(Money.Of(1000m, Currency.Inr))),
            SalaryStructureLine.Of(componentId, SalaryComponentFormula.FixedAmount(Money.Of(2000m, Currency.Inr))),
        };

        var result = SalaryStructure.Create(TenantId.New(), EmployeeId.New(), Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void A_Revised_Structure_Overlapping_The_Current_One_Should_Be_Rejected()
    {
        var tenantId = TenantId.New();
        var employeeId = EmployeeId.New();
        var lines = new[]
        {
            SalaryStructureLine.Of(SalaryComponentId.New(), SalaryComponentFormula.FixedAmount(Money.Of(50000m, Currency.Inr))),
        };

        var current = SalaryStructure.Create(tenantId, employeeId, Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null).Value;
        var revised = SalaryStructure.Create(tenantId, employeeId, Money.Zero(Currency.Inr), lines, new DateOnly(2026, 6, 1), null).Value;

        var result = EffectiveDatedTimeline.EnsureNoOverlap<SalaryStructureId, SalaryStructure>([current], revised);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Closing_The_Current_Structure_Allows_A_NonOverlapping_Revision()
    {
        var tenantId = TenantId.New();
        var employeeId = EmployeeId.New();
        var lines = new[]
        {
            SalaryStructureLine.Of(SalaryComponentId.New(), SalaryComponentFormula.FixedAmount(Money.Of(50000m, Currency.Inr))),
        };

        var current = SalaryStructure.Create(tenantId, employeeId, Money.Zero(Currency.Inr), lines, new DateOnly(2026, 1, 1), null).Value;
        current.EndOn(new DateOnly(2026, 5, 31));
        var revised = SalaryStructure.Create(tenantId, employeeId, Money.Zero(Currency.Inr), lines, new DateOnly(2026, 6, 1), null).Value;

        var result = EffectiveDatedTimeline.EnsureNoOverlap<SalaryStructureId, SalaryStructure>([current], revised);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void SalaryStructureLine_And_SalaryComponentFormula_Equality_Should_Compare_By_Value()
    {
        var componentId = SalaryComponentId.New();

        SalaryComponentFormula.FixedAmount(Money.Of(50000m, Currency.Inr))
            .Should().Be(SalaryComponentFormula.FixedAmount(Money.Of(50000m, Currency.Inr)));
        SalaryComponentFormula.PercentageOfComponent(componentId, 40m)
            .Should().Be(SalaryComponentFormula.PercentageOfComponent(componentId, 40m));
        SalaryComponentFormula.SumOfComponents([componentId]).Should().Be(SalaryComponentFormula.SumOfComponents([componentId]));

        var first = SalaryStructureLine.Of(componentId, SalaryComponentFormula.FixedAmount(Money.Of(50000m, Currency.Inr)));
        var second = SalaryStructureLine.Of(componentId, SalaryComponentFormula.FixedAmount(Money.Of(50000m, Currency.Inr)));

        first.Should().Be(second);
    }
}
