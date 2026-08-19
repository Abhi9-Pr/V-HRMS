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
            TenantId.New(), EmployeeId.New(), [], new DateOnly(2026, 1, 1), null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void GrossMonthly_Should_Sum_All_Lines()
    {
        var lines = new[]
        {
            SalaryStructureLine.Of(SalaryComponentId.New(), Money.Of(50000m, Currency.Inr)),
            SalaryStructureLine.Of(SalaryComponentId.New(), Money.Of(10000m, Currency.Inr)),
        };
        var structure = SalaryStructure.Create(
            TenantId.New(), EmployeeId.New(), lines, new DateOnly(2026, 1, 1), null).Value;

        structure.GrossMonthly().Should().Be(Money.Of(60000m, Currency.Inr));
    }

    [Fact]
    public void A_Revised_Structure_Overlapping_The_Current_One_Should_Be_Rejected()
    {
        var tenantId = TenantId.New();
        var employeeId = EmployeeId.New();
        var lines = new[] { SalaryStructureLine.Of(SalaryComponentId.New(), Money.Of(50000m, Currency.Inr)) };

        var current = SalaryStructure.Create(tenantId, employeeId, lines, new DateOnly(2026, 1, 1), null).Value;
        var revised = SalaryStructure.Create(tenantId, employeeId, lines, new DateOnly(2026, 6, 1), null).Value;

        var result = EffectiveDatedTimeline.EnsureNoOverlap<SalaryStructureId, SalaryStructure>([current], revised);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Closing_The_Current_Structure_Allows_A_NonOverlapping_Revision()
    {
        var tenantId = TenantId.New();
        var employeeId = EmployeeId.New();
        var lines = new[] { SalaryStructureLine.Of(SalaryComponentId.New(), Money.Of(50000m, Currency.Inr)) };

        var current = SalaryStructure.Create(tenantId, employeeId, lines, new DateOnly(2026, 1, 1), null).Value;
        current.EndOn(new DateOnly(2026, 5, 31));
        var revised = SalaryStructure.Create(tenantId, employeeId, lines, new DateOnly(2026, 6, 1), null).Value;

        var result = EffectiveDatedTimeline.EnsureNoOverlap<SalaryStructureId, SalaryStructure>([current], revised);

        result.IsSuccess.Should().BeTrue();
    }
}
