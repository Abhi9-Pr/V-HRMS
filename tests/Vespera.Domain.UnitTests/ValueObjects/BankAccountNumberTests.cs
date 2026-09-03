using FluentAssertions;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.ValueObjects;

public class BankAccountNumberTests
{
    [Fact]
    public void Create_Should_Succeed_For_A_Valid_Number()
    {
        BankAccountNumber.Create("123456789012").IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abc123456789")]
    public void Create_Should_Fail_For_Invalid_Numbers(string value)
    {
        BankAccountNumber.Create(value).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ToString_Should_Mask_All_But_The_Last_Four_Digits()
    {
        var account = BankAccountNumber.Create("123456789012").Value;

        account.ToString().Should().Be("********9012");
    }

    [Fact]
    public void Masked_Should_Never_Expose_The_Raw_Value_In_Full()
    {
        var account = BankAccountNumber.Create("123456789012").Value;

        account.Masked().Should().NotBe(account.Value);
    }

    [Fact]
    public void Instances_With_The_Same_Value_Should_Be_Equal()
    {
        var first = BankAccountNumber.Create("123456789012").Value;
        var second = BankAccountNumber.Create("123456789012").Value;
        var different = BankAccountNumber.Create("987654321098").Value;

        first.Should().Be(second);
        first.Should().NotBe(different);
    }
}
