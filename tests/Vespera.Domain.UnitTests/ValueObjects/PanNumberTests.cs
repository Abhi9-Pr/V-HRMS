using FluentAssertions;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.ValueObjects;

public class PanNumberTests
{
    [Fact]
    public void Create_Should_Succeed_For_A_Valid_Pan()
    {
        PanNumber.Create("ABCDE1234F").IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABCDE1234")]
    [InlineData("1234ABCDEF")]
    [InlineData("ABCDE12345")]
    public void Create_Should_Fail_For_Invalid_Pans(string value)
    {
        PanNumber.Create(value).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Normalize_To_Uppercase()
    {
        var result = PanNumber.Create("abcde1234f");

        result.Value.Value.Should().Be("ABCDE1234F");
    }

    [Fact]
    public void ToString_Should_Mask_The_Middle_Characters()
    {
        var pan = PanNumber.Create("ABCDE1234F").Value;

        pan.ToString().Should().Be("AB*******F");
    }

    [Fact]
    public void Instances_With_The_Same_Value_Should_Be_Equal()
    {
        var first = PanNumber.Create("ABCDE1234F").Value;
        var second = PanNumber.Create("ABCDE1234F").Value;
        var different = PanNumber.Create("PQRST5678G").Value;

        first.Should().Be(second);
        first.Should().NotBe(different);
    }
}
