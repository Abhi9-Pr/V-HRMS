namespace Vespera.Domain.Eis;

/// <summary>
/// Resolves an employee's assignment as of a given date from their <see cref="EmploymentHistory"/>
/// fact list. <see cref="EmploymentHistory"/> is a one-sided "effective from" record, not an
/// <see cref="Common.EffectiveDated{TId}"/> range, so <see cref="Common.EffectiveDatedTimeline"/>'s
/// generic <c>AsOf</c> doesn't apply here — the latest fact with <c>EffectiveFrom &lt;= date</c>
/// wins, the same "latest wins" idea applied to a fact list that has no explicit end date.
/// </summary>
public static class EmploymentHistoryTimeline
{
    public static EmploymentHistory? AsOf(IReadOnlyCollection<EmploymentHistory> history, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(history);

        return history
            .Where(entry => entry.EffectiveFrom <= date)
            .OrderByDescending(entry => entry.EffectiveFrom)
            .FirstOrDefault();
    }
}
