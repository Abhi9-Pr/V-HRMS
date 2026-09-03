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

    [Fact]
    public void Rename_Should_Update_The_Name_And_Touch_The_Audit_Trail()
    {
        var category = TicketCategory.Create(TenantId.New(), "Hardware", DepartmentId.New(), null, Now, "admin@vespera.test").Value;

        var result = category.Rename("Hardware & Peripherals", Now.AddMinutes(1), "lead@vespera.test");

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Hardware & Peripherals");
        category.ModifiedAt.Should().Be(Now.AddMinutes(1));
        category.ModifiedBy.Should().Be("lead@vespera.test");
    }

    [Fact]
    public void Delete_Should_Mark_The_Category_Deleted_And_Restore_Should_Reverse_It()
    {
        var category = TicketCategory.Create(TenantId.New(), "Hardware", DepartmentId.New(), null, Now, "admin@vespera.test").Value;

        var deleteResult = category.Delete(Now.AddMinutes(1), "admin@vespera.test");
        deleteResult.IsSuccess.Should().BeTrue();
        category.IsDeleted.Should().BeTrue();
        category.DeletedAt.Should().Be(Now.AddMinutes(1));
        category.DeletedBy.Should().Be("admin@vespera.test");

        var restoreResult = category.Restore();
        restoreResult.IsSuccess.Should().BeTrue();
        category.IsDeleted.Should().BeFalse();
        category.DeletedAt.Should().BeNull();
        category.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Delete_Should_Fail_When_Already_Deleted()
    {
        var category = TicketCategory.Create(TenantId.New(), "Hardware", DepartmentId.New(), null, Now, "admin@vespera.test").Value;
        category.Delete(Now.AddMinutes(1), "admin@vespera.test");

        var result = category.Delete(Now.AddMinutes(2), "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("entity.already_deleted");
    }

    [Fact]
    public void Restore_Should_Fail_When_Not_Deleted()
    {
        var category = TicketCategory.Create(TenantId.New(), "Hardware", DepartmentId.New(), null, Now, "admin@vespera.test").Value;

        var result = category.Restore();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("entity.not_deleted");
    }

    [Fact]
    public void Rename_Should_Fail_With_A_Blank_Name()
    {
        var category = TicketCategory.Create(TenantId.New(), "Hardware", DepartmentId.New(), null, Now, "admin@vespera.test").Value;

        var result = category.Rename("   ", Now.AddMinutes(1), "lead@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ticket_category.name_required");
        category.Name.Should().Be("Hardware");
    }

    [Fact]
    public void TicketCategoryId_New_Should_Generate_Distinct_Values()
    {
        TicketCategoryId.New().Should().NotBe(TicketCategoryId.New());
    }
}
