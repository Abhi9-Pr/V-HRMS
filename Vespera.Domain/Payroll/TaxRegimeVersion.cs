using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public readonly record struct TaxRegimeVersionId(Guid Value)
{
    public static TaxRegimeVersionId New() => new(Guid.NewGuid());
}

public enum TaxRegimeType
{
    Old,
    New,
}

public sealed class TaxSlab : ValueObject
{
    private TaxSlab(Money upTo, decimal ratePercent)
    {
        UpTo = upTo;
        RatePercent = ratePercent;
    }

    public Money UpTo { get; }

    public decimal RatePercent { get; }

    public static Result<TaxSlab> Create(Money upTo, decimal ratePercent)
    {
        if (ratePercent is < 0 or > 100)
        {
            return Result.Failure<TaxSlab>(Error.Validation("tax_slab.invalid_rate", "Rate percent must be between 0 and 100."));
        }

        return Result.Success(new TaxSlab(upTo, ratePercent));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UpTo;
        yield return RatePercent;
    }
}

public sealed class TaxRegimeVersion : AggregateRoot<TaxRegimeVersionId>, ITenantScoped
{
    private readonly List<TaxSlab> _slabs;

    private TaxRegimeVersion(
        TaxRegimeVersionId id, TenantId tenantId, TaxRegimeType regimeType, string financialYear, IReadOnlyList<TaxSlab> slabs)
        : base(id)
    {
        TenantId = tenantId;
        RegimeType = regimeType;
        FinancialYear = financialYear;
        _slabs = [.. slabs.OrderBy(slab => slab.UpTo.Amount)];
    }

    public TenantId TenantId { get; }

    public TaxRegimeType RegimeType { get; }

    public string FinancialYear { get; }

    public IReadOnlyList<TaxSlab> Slabs => _slabs.AsReadOnly();

    public static Result<TaxRegimeVersion> Create(
        TenantId tenantId, TaxRegimeType regimeType, string financialYear, IReadOnlyList<TaxSlab> slabs)
    {
        if (string.IsNullOrWhiteSpace(financialYear))
        {
            return Result.Failure<TaxRegimeVersion>(Error.Validation("tax_regime_version.fy_required", "Financial year is required."));
        }

        if (slabs is null || slabs.Count == 0)
        {
            return Result.Failure<TaxRegimeVersion>(Error.Validation("tax_regime_version.no_slabs", "At least one tax slab is required."));
        }

        return Result.Success(new TaxRegimeVersion(TaxRegimeVersionId.New(), tenantId, regimeType, financialYear.Trim(), slabs));
    }

    public Money CalculateTax(Money taxableIncome)
    {
        var tax = Money.Zero(taxableIncome.Currency);
        var previousLimit = Money.Zero(taxableIncome.Currency);

        foreach (var slab in _slabs)
        {
            if (taxableIncome <= previousLimit)
            {
                break;
            }

            var slabCeiling = slab.UpTo < taxableIncome ? slab.UpTo : taxableIncome;
            var slabAmount = slabCeiling - previousLimit;
            tax += Money.Of(slabAmount.Amount * slab.RatePercent / 100m, taxableIncome.Currency);
            previousLimit = slab.UpTo;
        }

        return tax;
    }
}
