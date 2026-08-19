using FluentAssertions;
using Vespera.Domain.Common;

namespace Vespera.Domain.UnitTests.Common;

public class ValueObjectTests
{
    [Fact]
    public void Value_Objects_With_The_Same_Components_Should_Be_Equal()
    {
        var first = new TestValueObject("a", 1);
        var second = new TestValueObject("a", 1);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Value_Objects_With_Different_Components_Should_Not_Be_Equal()
    {
        var first = new TestValueObject("a", 1);
        var second = new TestValueObject("a", 2);

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_Should_Be_Consistent_With_Equals()
    {
        var first = new TestValueObject("a", 1);
        var second = new TestValueObject("a", 1);

        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    private sealed class TestValueObject(string text, int number) : ValueObject
    {
        private string Text { get; } = text;

        private int Number { get; } = number;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Text;
            yield return Number;
        }
    }
}
