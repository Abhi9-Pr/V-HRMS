using FluentAssertions;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Adding_Money_In_The_Same_Currency_Should_Sum_The_Amounts()
    {
        var first = Money.Of(100m, Currency.Inr);
        var second = Money.Of(50m, Currency.Inr);

        var result = first + second;

        result.Should().Be(Money.Of(150m, Currency.Inr));
    }

    [Fact]
    public void Adding_Money_In_Different_Currencies_Should_Throw()
    {
        var inr = Money.Of(100m, Currency.Inr);
        var usd = Money.Of(100m, Currency.Usd);

        var adding = () => inr + usd;

        adding.Should().Throw<MoneyCurrencyMismatchException>();
    }

    [Fact]
    public void Subtracting_Money_In_Different_Currencies_Should_Throw()
    {
        var inr = Money.Of(100m, Currency.Inr);
        var usd = Money.Of(100m, Currency.Usd);

        var subtracting = () => inr - usd;

        subtracting.Should().Throw<MoneyCurrencyMismatchException>();
    }

    [Fact]
    public void Comparing_Money_In_Different_Currencies_Should_Throw()
    {
        var inr = Money.Of(100m, Currency.Inr);
        var usd = Money.Of(50m, Currency.Usd);

        var comparing = () => inr > usd;

        comparing.Should().Throw<MoneyCurrencyMismatchException>();
    }

    [Fact]
    public void Money_With_The_Same_Amount_And_Currency_Should_Be_Equal()
    {
        Money.Of(100m, Currency.Inr).Should().Be(Money.Of(100m, Currency.Inr));
    }

    [Fact]
    public void Zero_Should_Have_A_Zero_Amount_In_The_Given_Currency()
    {
        var zero = Money.Zero(Currency.Eur);

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be(Currency.Eur);
    }
}
