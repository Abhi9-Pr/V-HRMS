using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.IdentityAccess;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.IdentityAccess;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AssignRole_Should_Add_The_Role()
    {
        var user = CreateUser();
        var roleId = RoleId.New();

        var result = user.AssignRole(roleId, Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        user.RoleIds.Should().Contain(roleId);
    }

    [Fact]
    public void AssignRole_Should_Fail_When_Already_Assigned()
    {
        var user = CreateUser();
        var roleId = RoleId.New();
        user.AssignRole(roleId, Now, "admin@vespera.test");

        var result = user.AssignRole(roleId, Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RevokeRole_Should_Fail_When_Not_Assigned()
    {
        var user = CreateUser();

        var result = user.RevokeRole(RoleId.New(), Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Lock_Then_Reactivate_Should_Round_Trip_The_Status()
    {
        var user = CreateUser();

        user.Lock(Now, "admin@vespera.test").IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Locked);

        user.Reactivate(Now, "admin@vespera.test").IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public void RevokeRole_Should_Remove_An_Assigned_Role()
    {
        var user = CreateUser();
        var roleId = RoleId.New();
        user.AssignRole(roleId, Now, "admin@vespera.test");

        var result = user.RevokeRole(roleId, Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        user.RoleIds.Should().NotContain(roleId);
    }

    [Fact]
    public void Lock_Should_Fail_When_Already_Locked()
    {
        var user = CreateUser();
        user.Lock(Now, "admin@vespera.test");

        var result = user.Lock(Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.already_locked");
    }

    [Fact]
    public void Reactivate_Should_Fail_When_Already_Active()
    {
        var user = CreateUser();

        var result = user.Reactivate(Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.already_active");
    }

    [Fact]
    public void Deactivate_Should_Set_Status_To_Deactivated()
    {
        var user = CreateUser();

        var result = user.Deactivate(Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Deactivated);
    }

    [Fact]
    public void Deactivate_Should_Fail_When_Already_Deactivated()
    {
        var user = CreateUser();
        user.Deactivate(Now, "admin@vespera.test");

        var result = user.Deactivate(Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.already_deactivated");
    }

    [Fact]
    public void UserId_Instances_With_The_Same_Value_Should_Be_Equal()
    {
        var value = Guid.NewGuid();

        new UserId(value).Should().Be(new UserId(value));
        UserId.New().Should().NotBe(UserId.New());
    }

    private static User CreateUser()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        return User.Create(TenantId.New(), email, employeeId: null, Now, "admin@vespera.test");
    }
}
