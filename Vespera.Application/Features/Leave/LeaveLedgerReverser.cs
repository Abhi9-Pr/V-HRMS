using Vespera.Domain.Common;
using Vespera.Domain.Leave;

namespace Vespera.Application.Features.Leave;

/// <summary>Restores a <see cref="LeaveBalance"/> after a <see cref="LeaveRequest"/> that already
/// posted a <see cref="LeaveLedgerEntryType.Debit"/> at submission is rejected, withdrawn, or
/// cancelled after approval — the one place "rejections restore the ledger" is implemented, shared
/// by all three call sites so the reconciliation math lives in exactly one place. Computing the
/// still-outstanding amount (debited minus already reversed) rather than a fixed figure makes this
/// safe to call more than once for the same request: a second call reverses nothing.</summary>
public static class LeaveLedgerReverser
{
    public static Result ReverseDebit(
        LeaveBalance balance, LeaveRequestId leaveRequestId, string reason, DateTimeOffset occurredOn, string postedBy)
    {
        var debited = balance.Entries
            .Where(e => e.Type == LeaveLedgerEntryType.Debit && e.SourceType == "LeaveRequest" && e.SourceId == leaveRequestId.Value)
            .Sum(e => e.Amount);
        var alreadyReversed = balance.Entries
            .Where(e => e.Type == LeaveLedgerEntryType.Reversal && e.SourceType == "LeaveRequest" && e.SourceId == leaveRequestId.Value)
            .Sum(e => e.Amount);

        var outstanding = debited - alreadyReversed;
        if (outstanding <= 0)
        {
            return Result.Success();
        }

        var result = balance.PostEntry(
            LeaveLedgerEntryType.Reversal, LeaveLedgerDirection.Credit, outstanding, reason, occurredOn, postedBy,
            sourceType: "LeaveRequest", sourceId: leaveRequestId.Value);
        return result.IsFailure ? Result.Failure(result.Error) : Result.Success();
    }
}
