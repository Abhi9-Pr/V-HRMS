using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Expense;

public readonly record struct CurrencyRateId(Guid Value)
{
    public static CurrencyRateId New() => new(Guid.NewGuid());
}

public sealed class CurrencyRate : Entity<CurrencyRateId>, ITenantScoped
{
    private CurrencyRate(CurrencyRateId id, TenantId tenantId, Currency fromCurrency, Currency toCurrency, decimal rate, DateOnly effectiveDate)
        : base(id)
    {
        TenantId = tenantId;
        FromCurrency = fromCurrency;
        ToCurrency = toCurrency;
        Rate = rate;
        EffectiveDate = effectiveDate;
    }

    public TenantId TenantId { get; }

    public Currency FromCurrency { get; }

    public Currency ToCurrency { get; }

    public decimal Rate { get; }

    public DateOnly EffectiveDate { get; }

    public static Result<CurrencyRate> Create(TenantId tenantId, Currency fromCurrency, Currency toCurrency, decimal rate, DateOnly effectiveDate)
    {
        if (rate <= 0)
        {
            return Result.Failure<CurrencyRate>(Error.Validation("currency_rate.invalid_rate", "Rate must be positive."));
        }

        if (fromCurrency == toCurrency)
        {
            return Result.Failure<CurrencyRate>(Error.Validation("currency_rate.same_currency", "From and To currencies must differ."));
        }

        return Result.Success(new CurrencyRate(CurrencyRateId.New(), tenantId, fromCurrency, toCurrency, rate, effectiveDate));
    }

    public Result<Money> Convert(Money amount)
    {
        if (amount.Currency != FromCurrency)
        {
            return Result.Failure<Money>(
                Error.Validation("currency_rate.currency_mismatch", "Amount currency does not match this rate's From currency."));
        }

        return Result.Success(Money.Of(amount.Amount * Rate, ToCurrency));
    }
}
