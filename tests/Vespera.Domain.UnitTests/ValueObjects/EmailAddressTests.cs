using FluentAssertions;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.ValueObjects;

public class EmailAddressTests
{
    [Theory]
    [InlineData("person@example.com")]
    [InlineData("first.last@sub.example.co.in")]
    public void Create_Should_Succeed_For_Valid_Addresses(string value)
    {
        EmailAddress.Create(value).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    [InlineData("@missing-local.com")]
    public void Create_Should_Fail_For_Invalid_Addresses(string value)
    {
        EmailAddress.Create(value).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Equality_Should_Be_Case_Insensitive()
    {
        var first = EmailAddress.Create("Person@Example.com").Value;
        var second = EmailAddress.Create("person@example.com").Value;

        first.Should().Be(second);
    }
}
