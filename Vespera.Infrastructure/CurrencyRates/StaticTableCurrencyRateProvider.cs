using Microsoft.Extensions.Options;
using Vespera.Application.Abstractions.Services;

namespace Vespera.Infrastructure.CurrencyRates;

public sealed class StaticTableCurrencyRateProvider : ICurrencyRateProvider
{
    private readonly CurrencyRateOptions _options;

    public StaticTableCurrencyRateProvider(IOptions<CurrencyRateOptions> options)
    {
        _options = options.Value;
    }

    public Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, DateOnly asOf, CancellationToken cancellationToken)
    {
        if (_options.Rates.TryGetValue(fromCurrency, out var direct) && direct.TryGetValue(toCurrency, out var rate))
        {
            return Task.FromResult(rate);
        }

        if (_options.Rates.TryGetValue(toCurrency, out var inverse) && inverse.TryGetValue(fromCurrency, out var inverseRate)
            && inverseRate != 0)
        {
            return Task.FromResult(1 / inverseRate);
        }

        // No deployer-configured rate for this currency pair is a configuration error, not an
        // expected per-request failure — surfaced as a 500/ProblemDetails via the global exception
        // handler rather than a silently-guessed conversion.
        throw new InvalidOperationException($"No exchange rate is configured for '{fromCurrency}' -> '{toCurrency}' (or its inverse).");
    }
}
