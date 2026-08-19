namespace Vespera.Domain.Common;

public static class EffectiveDatedTimeline
{
    public static Result EnsureNoOverlap<TId, T>(IEnumerable<T> existing, T candidate)
        where T : EffectiveDated<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(candidate);

        var overlaps = existing.Any(item =>
            !Equals(item.Id, candidate.Id) && item.Overlaps(candidate.ValidFrom, candidate.ValidTo));

        return overlaps
            ? Result.Failure(Error.Conflict(
                "effective_dated.overlap",
                "The effective date range overlaps with an existing entry."))
            : Result.Success();
    }

    public static T? AsOf<TId, T>(IEnumerable<T> items, DateOnly date)
        where T : EffectiveDated<TId>
        where TId : notnull
    {
        ArgumentNullException.ThrowIfNull(items);

        return items.FirstOrDefault(item => item.IsActiveOn(date));
    }
}
