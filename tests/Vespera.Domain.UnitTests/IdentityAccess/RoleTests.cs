using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Domain.UnitTests.IdentityAccess;

public class RoleTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Succeed_And_Trim_The_Name()
    {
        var result = Role.Create(TenantId.New(), "  HR Manager  ", Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("HR Manager");
        result.Value.PermissionIds.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Name_Is_Blank(string name)
    {
        var result = Role.Create(TenantId.New(), name, Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("role.name_required");
    }

    [Fact]
    public void Grant_Should_Add_The_Permission()
    {
        var role = CreateRole();
        var permissionId = PermissionId.New();

        var result = role.Grant(permissionId, Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        role.PermissionIds.Should().Contain(permissionId);
    }

    [Fact]
    public void Grant_Should_Fail_When_Already_Granted()
    {
        var role = CreateRole();
        var permissionId = PermissionId.New();
        role.Grant(permissionId, Now, "admin@vespera.test");

        var result = role.Grant(permissionId, Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("role.permission_already_granted");
    }

    [Fact]
    public void Revoke_Should_Remove_The_Permission()
    {
        var role = CreateRole();
        var permissionId = PermissionId.New();
        role.Grant(permissionId, Now, "admin@vespera.test");

        var result = role.Revoke(permissionId, Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        role.PermissionIds.Should().NotContain(permissionId);
    }

    [Fact]
    public void Revoke_Should_Fail_When_Not_Granted()
    {
        var role = CreateRole();

        var result = role.Revoke(PermissionId.New(), Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("role.permission_not_granted");
    }

    private static Role CreateRole() => Role.Create(TenantId.New(), "HR Manager", Now, "admin@vespera.test").Value;
}
