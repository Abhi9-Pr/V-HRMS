using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Assets;

public enum DepreciationMethod
{
    StraightLine,
    DecliningBalance,
}

public sealed class DepreciationSchedule : ValueObject
{
    private DepreciationSchedule(DepreciationMethod method, int usefulLifeMonths, Money salvageValue)
    {
        Method = method;
        UsefulLifeMonths = usefulLifeMonths;
        SalvageValue = salvageValue;
    }

    public DepreciationMethod Method { get; }

    public int UsefulLifeMonths { get; }

    public Money SalvageValue { get; }

    public static Result<DepreciationSchedule> Create(DepreciationMethod method, int usefulLifeMonths, Money salvageValue)
    {
        if (usefulLifeMonths <= 0)
        {
            return Result.Failure<DepreciationSchedule>(
                Error.Validation("depreciation_schedule.invalid_useful_life", "Useful life must be a positive number of months."));
        }

        return Result.Success(new DepreciationSchedule(method, usefulLifeMonths, salvageValue));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Method;
        yield return UsefulLifeMonths;
        yield return SalvageValue;
    }
}
