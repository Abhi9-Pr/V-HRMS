namespace Vespera.Domain.Common;

public abstract class EffectiveDated<TId> : Entity<TId>
    where TId : notnull
{
    protected EffectiveDated(TId id, DateOnly validFrom, DateOnly? validTo)
        : base(id)
    {
        if (validTo is { } end && end < validFrom)
        {
            throw new ArgumentException("ValidTo cannot be before ValidFrom.", nameof(validTo));
        }

        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public DateOnly ValidFrom { get; }

    public DateOnly? ValidTo { get; private set; }

    public bool IsOpenEnded => ValidTo is null;

    public bool IsActiveOn(DateOnly date) => date >= ValidFrom && (ValidTo is null || date <= ValidTo);

    public bool Overlaps(DateOnly otherFrom, DateOnly? otherTo) =>
        ValidFrom <= (otherTo ?? DateOnly.MaxValue) && otherFrom <= (ValidTo ?? DateOnly.MaxValue);

    protected Result Close(DateOnly validTo)
    {
        if (validTo < ValidFrom)
        {
            return Result.Failure(Error.Validation("effective_dated.invalid_close", "ValidTo cannot be before ValidFrom."));
        }

        ValidTo = validTo;
        return Result.Success();
    }
}
