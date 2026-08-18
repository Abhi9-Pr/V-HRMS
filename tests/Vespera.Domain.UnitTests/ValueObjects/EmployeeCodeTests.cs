using FluentAssertions;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.ValueObjects;

public class EmployeeCodeTests
{
    [Theory]
    [InlineData("EMP-001")]
    [InlineData("A1")]
    public void Create_Should_Succeed_For_Valid_Codes(string value)
    {
        EmployeeCode.Create(value).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has space")]
    [InlineData("has_underscore")]
    public void Create_Should_Fail_For_Invalid_Codes(string value)
    {
        EmployeeCode.Create(value).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Trim_Whitespace()
    {
        var result = EmployeeCode.Create("  EMP-001  ");

        result.Value.Value.Should().Be("EMP-001");
    }
}
