using FluentAssertions;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Domain.UnitTests.IdentityAccess;

public class TenantTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Should_Succeed_In_Trial_Status_And_Trim_Name_And_Code()
    {
        var result = Tenant.Create("  Demo Corp  ", "  DEMO  ", Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Demo Corp");
        result.Value.Code.Should().Be("DEMO");
        result.Value.Status.Should().Be(TenantStatus.Trial);
        result.Value.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Name_Is_Blank(string name)
    {
        var result = Tenant.Create(name, "DEMO", Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tenant.name_required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Code_Is_Blank(string code)
    {
        var result = Tenant.Create("Demo Corp", code, Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tenant.code_required");
    }

    [Fact]
    public void Rename_Should_Succeed_And_Trim()
    {
        var tenant = CreateTenant();

        var result = tenant.Rename("  New Name  ", Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        tenant.Name.Should().Be("New Name");
        tenant.ModifiedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_Should_Fail_When_Blank(string name)
    {
        var tenant = CreateTenant();

        var result = tenant.Rename(name, Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tenant.name_required");
    }

    [Fact]
    public void Reactivate_From_Trial_Should_Succeed()
    {
        var tenant = CreateTenant();

        var result = tenant.Reactivate(Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public void Reactivate_Should_Fail_When_Already_Active()
    {
        var tenant = CreateTenant();
        tenant.Reactivate(Now, "admin@vespera.test");

        var result = tenant.Reactivate(Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tenant.already_active");
    }

    [Fact]
    public void Suspend_Should_Succeed_From_Active()
    {
        var tenant = CreateTenant();
        tenant.Reactivate(Now, "admin@vespera.test");

        var result = tenant.Suspend(Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        tenant.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public void Suspend_Should_Fail_When_Already_Suspended()
    {
        var tenant = CreateTenant();
        tenant.Reactivate(Now, "admin@vespera.test");
        tenant.Suspend(Now, "admin@vespera.test");

        var result = tenant.Suspend(Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tenant.already_suspended");
    }

    [Fact]
    public void Delete_Should_Mark_Deleted_With_Metadata()
    {
        var tenant = CreateTenant();

        var result = tenant.Delete(Now, "admin@vespera.test");

        result.IsSuccess.Should().BeTrue();
        tenant.IsDeleted.Should().BeTrue();
        tenant.DeletedAt.Should().Be(Now);
        tenant.DeletedBy.Should().Be("admin@vespera.test");
    }

    [Fact]
    public void Delete_Should_Fail_When_Already_Deleted()
    {
        var tenant = CreateTenant();
        tenant.Delete(Now, "admin@vespera.test");

        var result = tenant.Delete(Now, "admin@vespera.test");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("tenant.already_deleted");
    }

    private static Tenant CreateTenant() => Tenant.Create("Demo Corp", "DEMO", Now, "admin@vespera.test").Value;
}
