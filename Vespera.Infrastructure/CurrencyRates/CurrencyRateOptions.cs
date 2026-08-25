namespace Vespera.Infrastructure.CurrencyRates;

/// <summary>Fallback exchange-rate table used when no cached <c>CurrencyRate</c> exists for a given
/// date — bound from configuration section <see cref="SectionName"/>, nested
/// <c>fromCurrency -> toCurrency -> rate</c> (a flat "USD:INR" key would collide with .NET
/// configuration's own ':' hierarchy separator, so the pair is two levels of nesting instead).
/// The seam for a real FX-rate API later: swap <see cref="StaticTableCurrencyRateProvider"/> for a
/// live provider with one DI registration line (OCP), contract unchanged.</summary>
public sealed class CurrencyRateOptions
{
    public const string SectionName = "Vespera:CurrencyRates";

    public Dictionary<string, Dictionary<string, decimal>> Rates { get; init; } = [];
}
