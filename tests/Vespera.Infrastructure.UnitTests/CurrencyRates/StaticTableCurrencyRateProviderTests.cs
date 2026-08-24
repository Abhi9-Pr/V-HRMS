using FluentAssertions;
using Microsoft.Extensions.Options;
using Vespera.Infrastructure.CurrencyRates;

namespace Vespera.Infrastructure.UnitTests.CurrencyRates;

public class StaticTableCurrencyRateProviderTests
{
    private static StaticTableCurrencyRateProvider CreateProvider(Dictionary<string, Dictionary<string, decimal>> rates) =>
        new(Options.Create(new CurrencyRateOptions { Rates = rates }));

    [Fact]
    public async Task GetRateAsync_Should_Return_The_Configured_Rate()
    {
        var provider = CreateProvider(new() { ["USD"] = new() { ["INR"] = 83m } });

        var rate = await provider.GetRateAsync("USD", "INR", new DateOnly(2026, 1, 10), CancellationToken.None);

        rate.Should().Be(83m);
    }

    [Fact]
    public async Task GetRateAsync_Should_Fall_Back_To_The_Inverse_Rate_When_Only_That_Is_Configured()
    {
        var provider = CreateProvider(new() { ["INR"] = new() { ["USD"] = 0.01m } });

        var rate = await provider.GetRateAsync("USD", "INR", new DateOnly(2026, 1, 10), CancellationToken.None);

        rate.Should().Be(100m);
    }

    [Fact]
    public async Task GetRateAsync_Should_Throw_When_No_Rate_Is_Configured_Either_Way()
    {
        var provider = CreateProvider([]);

        var act = async () => await provider.GetRateAsync("USD", "INR", new DateOnly(2026, 1, 10), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
