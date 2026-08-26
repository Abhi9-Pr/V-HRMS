using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave.Events;

namespace Vespera.Domain.Leave;

public readonly record struct LeaveBalanceId(Guid Value)
{
    public static LeaveBalanceId New() => new(Guid.NewGuid());
}

/// <summary>
/// One continuous, per-(tenant, employee, leave type) balance. There is no mutable balance field:
/// <see cref="Available"/>, <see cref="Accrued"/>, <see cref="Used"/> and <see cref="CarriedForward"/>
/// are all projections computed over <see cref="Entries"/>, the append-only ledger. The only way to
/// move the balance is <see cref="PostEntry"/>, which mints one immutable <see cref="LeaveLedgerEntry"/>.
/// </summary>
public sealed class LeaveBalance : AggregateRoot<LeaveBalanceId>, ITenantScoped
{
    private readonly List<LeaveLedgerEntry> _entries = [];

    private LeaveBalance(LeaveBalanceId id, TenantId tenantId, EmployeeId employeeId, LeaveTypeId leaveTypeId)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public LeaveTypeId LeaveTypeId { get; }

    public IReadOnlyList<LeaveLedgerEntry> Entries => _entries.AsReadOnly();

    public decimal Available => _entries.Sum(e => e.SignedAmount);

    public decimal Accrued => _entries.Where(e => e.Type == LeaveLedgerEntryType.Accrual).Sum(e => e.Amount);

    public decimal CarriedForward => _entries.Where(e => e.Type == LeaveLedgerEntryType.CarryForward).Sum(e => e.Amount);

    public decimal Used =>
        _entries.Where(e => e.Type == LeaveLedgerEntryType.Debit).Sum(e => e.Amount)
        - _entries.Where(e => e.Type == LeaveLedgerEntryType.Reversal).Sum(e => e.Amount);

    public static LeaveBalance Open(TenantId tenantId, EmployeeId employeeId, LeaveTypeId leaveTypeId) =>
        new(LeaveBalanceId.New(), tenantId, employeeId, leaveTypeId);

    /// <summary>
    /// Appends one immutable ledger entry and returns it — the only way to change the balance.
    /// <paramref name="minimumAllowedBalance"/>, when supplied, rejects a debit that would push
    /// <see cref="Available"/> below that floor (a policy's negative-balance cap); pass null to allow
    /// an unbounded debit — loss-of-pay routing already keeps paid debits from going negative by
    /// construction, so this guard exists only for the explicit "allow negative up to N" policy.
    /// </summary>
    public Result<LeaveLedgerEntry> PostEntry(
        LeaveLedgerEntryType type, LeaveLedgerDirection direction, decimal amount, string reason,
        DateTimeOffset occurredOn, string postedBy, string? sourceType = null, Guid? sourceId = null,
        string? periodKey = null, decimal? minimumAllowedBalance = null)
    {
        if (amount <= 0)
        {
            return Result.Failure<LeaveLedgerEntry>(
                Error.Validation("leave_balance.invalid_amount", "Ledger entry amount must be positive."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<LeaveLedgerEntry>(
                Error.Validation("leave_balance.reason_required", "A reason is required for every ledger posting."));
        }

        if (direction == LeaveLedgerDirection.Debit && minimumAllowedBalance is { } floor && Available - amount < floor)
        {
            return Result.Failure<LeaveLedgerEntry>(Error.Conflict(
                "leave_balance.negative_balance_not_allowed", "This debit would breach the negative-balance policy."));
        }

        var entry = new LeaveLedgerEntry(
            LeaveLedgerEntryId.New(), type, direction, amount, reason.Trim(), sourceType, sourceId, periodKey, occurredOn, postedBy);
        _entries.Add(entry);
        return Result.Success(entry);
    }

    /// <summary>Debits the balance for a cash payout and raises <see cref="LeaveEncashed"/> — the
    /// one <see cref="PostEntry"/> caller that needs an event, since payroll (and any notification)
    /// only cares about encashments, not every ledger posting.</summary>
    public Result<LeaveLedgerEntry> Encash(decimal days, DateTimeOffset occurredOn, string postedBy)
    {
        var result = PostEntry(LeaveLedgerEntryType.Encashment, LeaveLedgerDirection.Debit, days, "Encashment", occurredOn, postedBy);
        if (result.IsFailure)
        {
            return result;
        }

        Raise(new LeaveEncashed(Id, TenantId, EmployeeId, LeaveTypeId, days, occurredOn));
        return result;
    }
}
