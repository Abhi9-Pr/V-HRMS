using FluentAssertions;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+14155552671")]
    [InlineData("+919876543210")]
    public void Create_Should_Succeed_For_Valid_E164_Numbers(string value)
    {
        PhoneNumber.Create(value).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("9876543210")]
    [InlineData("+0123456789")]
    [InlineData("not-a-number")]
    public void Create_Should_Fail_For_Invalid_Numbers(string value)
    {
        PhoneNumber.Create(value).IsFailure.Should().BeTrue();
    }
}
