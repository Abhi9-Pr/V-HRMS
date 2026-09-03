using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.UnitTests.Eis;

public class DepartmentTests
{
    private static readonly TenantId TenantId = TenantId.New();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Succeed_And_Trim_Name_And_Code()
    {
        var result = Department.Create(TenantId, "  Engineering  ", "  ENG  ", null, Now, "system");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Engineering");
        result.Value.Code.Should().Be("ENG");
        result.Value.ParentDepartmentId.Should().BeNull();
        result.Value.HeadEmployeeId.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Name_Is_Missing(string name)
    {
        var result = Department.Create(TenantId, name, "ENG", null, Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("department.name_required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Code_Is_Missing(string code)
    {
        var result = Department.Create(TenantId, "Engineering", code, null, Now, "system");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("department.code_required");
    }

    [Fact]
    public void Rename_Should_Update_Name_And_Trim_It()
    {
        var department = Department.Create(TenantId, "Engineering", "ENG", null, Now, "system").Value;

        var result = department.Rename("  Product Engineering  ", Now.AddDays(1), "admin");

        result.IsSuccess.Should().BeTrue();
        department.Name.Should().Be("Product Engineering");
    }

    [Fact]
    public void Rename_Should_Fail_When_Name_Is_Missing()
    {
        var department = Department.Create(TenantId, "Engineering", "ENG", null, Now, "system").Value;

        var result = department.Rename(string.Empty, Now.AddDays(1), "admin");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("department.name_required");
    }

    [Fact]
    public void Reparent_Should_Set_The_Parent_Department()
    {
        var department = Department.Create(TenantId, "Engineering", "ENG", null, Now, "system").Value;
        var parentId = DepartmentId.New();

        var result = department.Reparent(parentId, Now.AddDays(1), "admin");

        result.IsSuccess.Should().BeTrue();
        department.ParentDepartmentId.Should().Be(parentId);
    }

    [Fact]
    public void Reparent_Should_Fail_When_Assigning_The_Department_As_Its_Own_Parent()
    {
        var department = Department.Create(TenantId, "Engineering", "ENG", null, Now, "system").Value;

        var result = department.Reparent(department.Id, Now.AddDays(1), "admin");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("department.self_parent");
    }

    [Fact]
    public void Reparent_Should_Allow_Clearing_The_Parent()
    {
        var department = Department.Create(TenantId, "Engineering", "ENG", DepartmentId.New(), Now, "system").Value;

        var result = department.Reparent(null, Now.AddDays(1), "admin");

        result.IsSuccess.Should().BeTrue();
        department.ParentDepartmentId.Should().BeNull();
    }

    [Fact]
    public void AssignHead_Should_Set_The_Head_Employee()
    {
        var department = Department.Create(TenantId, "Engineering", "ENG", null, Now, "system").Value;
        var headEmployeeId = EmployeeId.New();

        var result = department.AssignHead(headEmployeeId, Now.AddDays(1), "admin");

        result.IsSuccess.Should().BeTrue();
        department.HeadEmployeeId.Should().Be(headEmployeeId);
    }
}
