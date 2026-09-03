using FluentAssertions;
using Vespera.Domain.IdentityAccess;

namespace Vespera.Domain.UnitTests.IdentityAccess;

public class PermissionTests
{
    [Fact]
    public void Create_Should_Succeed_And_Trim_Code_And_Description()
    {
        var result = Permission.Create("  Employees.Read  ", "  Read employee records  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("Employees.Read");
        result.Value.Description.Should().Be("Read employee records");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Code_Is_Blank(string code)
    {
        var result = Permission.Create(code, "Read employee records");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission.code_required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Fail_When_Description_Is_Blank(string description)
    {
        var result = Permission.Create("Employees.Read", description);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission.description_required");
    }

    [Fact]
    public void UpdateDescription_Should_Succeed_And_Trim()
    {
        var permission = Permission.Create("Employees.Read", "Read employee records").Value;

        var result = permission.UpdateDescription("  Updated description  ");

        result.IsSuccess.Should().BeTrue();
        permission.Description.Should().Be("Updated description");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDescription_Should_Fail_When_Blank(string description)
    {
        var permission = Permission.Create("Employees.Read", "Read employee records").Value;

        var result = permission.UpdateDescription(description);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("permission.description_required");
    }
}
