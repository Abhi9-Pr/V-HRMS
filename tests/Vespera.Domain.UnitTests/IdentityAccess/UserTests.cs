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

    private static User CreateUser()
    {
        var email = EmailAddress.Create("user@vespera.test").Value;
        return User.Create(TenantId.New(), email, employeeId: null, Now, "admin@vespera.test");
    }
}
