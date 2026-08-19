using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Expense;

public class CurrencyRateTests
{
    [Fact]
    public void Convert_Should_Apply_The_Rate()
    {
        var rate = CurrencyRate.Create(TenantId.New(), Currency.Usd, Currency.Inr, 83m, new DateOnly(2026, 1, 1)).Value;

        var result = rate.Convert(Money.Of(10m, Currency.Usd));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(Money.Of(830m, Currency.Inr));
    }

    [Fact]
    public void Convert_Should_Fail_When_The_Amount_Currency_Does_Not_Match_FromCurrency()
    {
        var rate = CurrencyRate.Create(TenantId.New(), Currency.Usd, Currency.Inr, 83m, new DateOnly(2026, 1, 1)).Value;

        var result = rate.Convert(Money.Of(10m, Currency.Eur));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Reject_The_Same_Currency_On_Both_Sides()
    {
        var result = CurrencyRate.Create(TenantId.New(), Currency.Inr, Currency.Inr, 1m, new DateOnly(2026, 1, 1));

        result.IsFailure.Should().BeTrue();
    }
}
