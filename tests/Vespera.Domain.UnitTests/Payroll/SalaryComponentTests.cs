using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Payroll;

namespace Vespera.Domain.UnitTests.Payroll;

public class SalaryComponentTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Succeed_And_Trim_The_Name()
    {
        var result = SalaryComponent.Create(TenantId, "  Basic  ", SalaryComponentType.Earning, isTaxable: true, Now, "seed");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Basic");
        result.Value.ComponentType.Should().Be(SalaryComponentType.Earning);
        result.Value.IsTaxable.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Fail_When_Name_Is_Empty()
    {
        var result = SalaryComponent.Create(TenantId, "   ", SalaryComponentType.Earning, true, Now, "seed");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("salary_component.name_required");
    }

    [Fact]
    public void Rename_Should_Trim_And_Update_The_Name()
    {
        var component = SalaryComponent.Create(TenantId, "Basic", SalaryComponentType.Earning, true, Now, "seed").Value;

        var result = component.Rename("  House Rent Allowance  ", Now, "editor");

        result.IsSuccess.Should().BeTrue();
        component.Name.Should().Be("House Rent Allowance");
    }

    [Fact]
    public void Rename_Should_Fail_When_The_New_Name_Is_Empty()
    {
        var component = SalaryComponent.Create(TenantId, "Basic", SalaryComponentType.Earning, true, Now, "seed").Value;

        var result = component.Rename(string.Empty, Now, "editor");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("salary_component.name_required");
        component.Name.Should().Be("Basic");
    }

    [Fact]
    public void UpdateTaxability_Should_Change_IsTaxable()
    {
        var component = SalaryComponent.Create(TenantId, "Basic", SalaryComponentType.Earning, isTaxable: true, Now, "seed").Value;

        var result = component.UpdateTaxability(false, Now, "editor");

        result.IsSuccess.Should().BeTrue();
        component.IsTaxable.Should().BeFalse();
    }
}
