using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Expenses;

/// <summary>Maps the closed <see cref="Currency"/> enum to/from the ISO 4217 codes
/// <see cref="Abstractions.Services.ICurrencyRateProvider"/> speaks.</summary>
public static class CurrencyCodeMapper
{
    public static string ToIsoCode(this Currency currency) => currency switch
    {
        Currency.Inr => "INR",
        Currency.Usd => "USD",
        Currency.Eur => "EUR",
        Currency.Gbp => "GBP",
        Currency.Aed => "AED",
        Currency.Sgd => "SGD",
        _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unsupported currency."),
    };
}
