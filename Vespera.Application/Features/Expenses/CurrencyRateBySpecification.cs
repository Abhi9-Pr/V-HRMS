using System.Linq.Expressions;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Application.Features.Expenses;

/// <summary>Looks up a cached daily rate for an exact (from, to, date) triple — the "cached daily-rate
/// store" read side. A miss falls back to <see cref="Abstractions.Services.ICurrencyRateProvider"/>.</summary>
public sealed class CurrencyRateBySpecification : ISpecification<CurrencyRate>
{
    public CurrencyRateBySpecification(TenantId tenantId, Currency fromCurrency, Currency toCurrency, DateOnly effectiveDate)
    {
        Criteria = rate =>
            rate.TenantId == tenantId && rate.FromCurrency == fromCurrency && rate.ToCurrency == toCurrency
            && rate.EffectiveDate == effectiveDate;
    }

    public Expression<Func<CurrencyRate, bool>>? Criteria { get; }

    public IReadOnlyList<Expression<Func<CurrencyRate, object>>> Includes { get; } = [];

    public IReadOnlyList<(Expression<Func<CurrencyRate, object>> KeySelector, bool Descending)> OrderBy { get; } = [];

    public (int Skip, int Take)? Paging => null;
}
