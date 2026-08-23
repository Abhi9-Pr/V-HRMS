using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public readonly record struct PayrollRunId(Guid Value)
{
    public static PayrollRunId New() => new(Guid.NewGuid());
}

/// <summary>
/// <c>Draft -&gt; AttendanceFrozen -&gt; DryRun -&gt; Review -&gt; Approved -&gt; Finalized -&gt; Published</c>,
/// strictly linear except that <see cref="PayrollRun.RecomputeLines"/> may be called again from
/// <see cref="DryRun"/> or <see cref="Review"/> (landing back in <see cref="DryRun"/>) to correct
/// data before submitting for review again — every other transition is one-way. Once a run reaches
/// <see cref="Approved"/>, nothing can touch <see cref="PayrollRun.Lines"/> again: that is what
/// "seals the ledger" means concretely. A correction after that point is a separate arrears run,
/// never a mutation of this one.
/// </summary>
public enum PayrollRunStatus
{
    Draft,
    AttendanceFrozen,
    DryRun,
    Review,
    Approved,
    Finalized,
    Published,
}

/// <summary>One employee's computed totals for a <see cref="RecomputeLines"/> call — the rollup a
/// <c>PayrollComputationEngine</c> hands back after running the rules pipeline for that employee.</summary>
public sealed record PayrollLineInput(EmployeeId EmployeeId, Money Gross, Money Deductions, Money Net, decimal LossOfPayDays);

public sealed class PayrollRun : AuditableTenantAggregateRoot<PayrollRunId>
{
    private readonly List<PayrollLine> _lines = [];

    private PayrollRun(PayrollRunId id, TenantId tenantId, int month, int year, DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Month = month;
        Year = year;
        Status = PayrollRunStatus.Draft;
    }

    public int Month { get; }

    public int Year { get; }

    public PayrollRunStatus Status { get; private set; }

    public IReadOnlyCollection<PayrollLine> Lines => _lines.AsReadOnly();

    /// <summary>Who most recently ran the dry-run compute that produced the numbers currently
    /// under review/approval — the maker-checker boundary this phase adds on top of "who created
    /// the run": someone can open a run and a different person can be the one whose numbers need
    /// independent checking. Null until the first <see cref="RecomputeLines"/> call.</summary>
    public string? DryRunExecutedBy { get; private set; }

    /// <summary>Set only when <see cref="FreezeAttendance"/> was called before the tenant's
    /// configured freeze day — an early freeze is legal but must be explained and attributed.</summary>
    public string? FreezeOverriddenBy { get; private set; }

    public string? FreezeOverrideReason { get; private set; }

    public static Result<PayrollRun> Open(TenantId tenantId, int month, int year, DateTimeOffset occurredOn, string createdBy)
    {
        if (month is < 1 or > 12)
        {
            return Result.Failure<PayrollRun>(Error.Validation("payroll_run.invalid_month", "Month must be between 1 and 12."));
        }

        return Result.Success(new PayrollRun(PayrollRunId.New(), tenantId, month, year, occurredOn, createdBy));
    }

    /// <summary><paramref name="freezeDay"/> is the tenant's configured day-of-month (see
    /// <c>PayrollSettings.AttendanceFreezeDay</c>); freezing before it requires a non-blank
    /// <paramref name="overrideReason"/>, which is recorded here (and picked up by the ordinary
    /// write-audit trail like any other field change — no separate audit plumbing needed).</summary>
    public Result FreezeAttendance(
        DateOnly asOf, int freezeDay, DateTimeOffset occurredOn, string freezeBy, string? overrideReason = null)
    {
        if (Status != PayrollRunStatus.Draft)
        {
            return Result.Failure(Error.Conflict("payroll_run.not_draft", "Attendance can only be frozen from Draft."));
        }

        var isEarly = asOf.Day < freezeDay;
        if (isEarly && string.IsNullOrWhiteSpace(overrideReason))
        {
            return Result.Failure(Error.Validation(
                "payroll_run.freeze_override_reason_required",
                $"Freezing before day {freezeDay} of the month requires an override reason."));
        }

        Status = PayrollRunStatus.AttendanceFrozen;
        FreezeOverriddenBy = isEarly ? freezeBy : null;
        FreezeOverrideReason = isEarly ? overrideReason!.Trim() : null;
        Touch(occurredOn, freezeBy);
        Raise(new PayrollRunAttendanceFrozen(Id, TenantId, isEarly, FreezeOverrideReason, occurredOn));
        return Result.Success();
    }

    /// <summary>Clears and repopulates <see cref="Lines"/> from a fresh pipeline run — the "dry
    /// run" step, and also how a correction discovered in <see cref="Review"/> gets re-computed
    /// before going back for review. Not callable once the run reaches <see cref="Approved"/> or
    /// later, by construction: there is no path back to this method from those statuses.</summary>
    public Result RecomputeLines(IReadOnlyList<PayrollLineInput> inputs, string executedBy, DateTimeOffset occurredOn)
    {
        if (Status is not (PayrollRunStatus.AttendanceFrozen or PayrollRunStatus.DryRun or PayrollRunStatus.Review))
        {
            return Result.Failure(Error.Conflict(
                "payroll_run.cannot_recompute", "Lines can only be (re)computed while attendance is frozen, or during dry-run/review."));
        }

        _lines.Clear();
        foreach (var input in inputs)
        {
            _lines.Add(new PayrollLine(PayrollLineId.New(), input.EmployeeId, input.Gross, input.Deductions, input.Net, input.LossOfPayDays));
        }

        Status = PayrollRunStatus.DryRun;
        DryRunExecutedBy = executedBy;
        Touch(occurredOn, executedBy);
        Raise(new PayrollRunDryRunCompleted(Id, TenantId, _lines.Count, executedBy, occurredOn));
        return Result.Success();
    }

    public Result SubmitForReview()
    {
        if (Status != PayrollRunStatus.DryRun)
        {
            return Result.Failure(Error.Conflict("payroll_run.not_dry_run", "Only a computed dry-run can be submitted for review."));
        }

        if (_lines.Count == 0)
        {
            return Result.Failure(Error.Validation("payroll_run.empty", "Cannot submit an empty payroll run for review."));
        }

        Status = PayrollRunStatus.Review;
        return Result.Success();
    }

    public Result Approve(string approvedBy, DateTimeOffset occurredOn)
    {
        if (Status != PayrollRunStatus.Review)
        {
            return Result.Failure(Error.Conflict("payroll_run.not_in_review", "Only a run under review can be approved."));
        }

        Status = PayrollRunStatus.Approved;
        Touch(occurredOn, approvedBy);
        Raise(new PayrollRunApproved(Id, TenantId, approvedBy, occurredOn));
        return Result.Success();
    }

    public Result Finalize(DateTimeOffset occurredOn)
    {
        if (Status == PayrollRunStatus.Finalized || Status == PayrollRunStatus.Published)
        {
            return Result.Failure(Error.Conflict("payroll_run.already_finalized", "This payroll run has already been finalized."));
        }

        if (Status != PayrollRunStatus.Approved)
        {
            return Result.Failure(Error.Conflict("payroll_run.not_approved", "Only an approved payroll run can be finalized."));
        }

        if (_lines.Count == 0)
        {
            return Result.Failure(Error.Validation("payroll_run.empty", "Cannot finalize a payroll run with no lines."));
        }

        Status = PayrollRunStatus.Finalized;
        Raise(new PayrollFinalized(Id, Month, Year, _lines.Count, occurredOn));
        return Result.Success();
    }

    public Result Publish(DateTimeOffset occurredOn, string publishedBy)
    {
        if (Status != PayrollRunStatus.Finalized)
        {
            return Result.Failure(Error.Conflict("payroll_run.not_finalized", "Only a finalized payroll run can be published."));
        }

        Status = PayrollRunStatus.Published;
        Touch(occurredOn, publishedBy);
        Raise(new PayrollRunPublished(Id, TenantId, _lines.Count, occurredOn));
        return Result.Success();
    }
}
