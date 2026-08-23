using Vespera.Domain.Common;

namespace Vespera.Domain.Leave;

public readonly record struct LeaveLedgerEntryId(Guid Value)
{
    public static LeaveLedgerEntryId New() => new(Guid.NewGuid());
}

public enum LeaveLedgerEntryType
{
    Accrual,
    CarryForward,
    Debit,
    Reversal,
    Adjustment,
    Encashment,
}

public enum LeaveLedgerDirection
{
    Credit,
    Debit,
}

/// <summary>
/// One immutable posting against a <see cref="LeaveBalance"/> — the durable audit trail. Never
/// constructed directly: only <see cref="LeaveBalance.PostEntry"/> can mint one, so the balance's
/// computed totals (<see cref="LeaveBalance.Available"/> etc.) can never drift from this ledger.
/// </summary>
public sealed class LeaveLedgerEntry : Entity<LeaveLedgerEntryId>
{
    internal LeaveLedgerEntry(
        LeaveLedgerEntryId id, LeaveLedgerEntryType type, LeaveLedgerDirection direction, decimal amount,
        string reason, string? sourceType, Guid? sourceId, string? periodKey, DateTimeOffset occurredOn, string postedBy)
        : base(id)
    {
        Type = type;
        Direction = direction;
        Amount = amount;
        Reason = reason;
        SourceType = sourceType;
        SourceId = sourceId;
        PeriodKey = periodKey;
        OccurredOn = occurredOn;
        PostedBy = postedBy;
    }

    public LeaveLedgerEntryType Type { get; }

    public LeaveLedgerDirection Direction { get; }

    public decimal Amount { get; }

    public decimal SignedAmount => Direction == LeaveLedgerDirection.Credit ? Amount : -Amount;

    public string Reason { get; }

    /// <summary>E.g. "LeaveRequest" — the aggregate this entry was posted on behalf of, if any.</summary>
    public string? SourceType { get; }

    public Guid? SourceId { get; }

    /// <summary>Set only on <see cref="LeaveLedgerEntryType.Accrual"/> entries, e.g. "2026-08" — lets
    /// the monthly accrual job check "have I already posted this employee's accrual for this month"
    /// before posting again, so re-running the job within the same month is a no-op.</summary>
    public string? PeriodKey { get; }

    public DateTimeOffset OccurredOn { get; }

    public string PostedBy { get; }
}
