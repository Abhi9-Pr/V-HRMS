using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Helpdesk;

namespace Vespera.Domain.UnitTests.Helpdesk;

public class TicketCategoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Fail_With_A_Blank_Name()
    {
        var result = TicketCategory.Create(TenantId.New(), " ", DepartmentId.New(), null, Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Succeed_And_Carry_The_Department()
    {
        var departmentId = DepartmentId.New();

        var result = TicketCategory.Create(TenantId.New(), "Hardware", departmentId, null, Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        result.Value.DepartmentId.Should().Be(departmentId);
    }

    [Fact]
    public void AssignDefaultSla_Should_Set_The_Policy()
    {
        var category = TicketCategory.Create(TenantId.New(), "Hardware", DepartmentId.New(), null, Now, "admin@vespera.test").Value;
        var policyId = SlaPolicyId.New();

        var result = category.AssignDefaultSla(policyId, Now.AddMinutes(1), "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        category.DefaultSlaPolicyId.Should().Be(policyId);
    }
}
