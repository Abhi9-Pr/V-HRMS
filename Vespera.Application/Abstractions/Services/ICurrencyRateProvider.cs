namespace Vespera.Application.Abstractions.Services;

public interface ICurrencyRateProvider
{
    public Task<decimal> GetRateAsync(string fromCurrency, string toCurrency, DateOnly asOf, CancellationToken cancellationToken);
}
