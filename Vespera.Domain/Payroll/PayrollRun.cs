using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Payroll.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Payroll;

public readonly record struct PayrollRunId(Guid Value)
{
    public static PayrollRunId New() => new(Guid.NewGuid());
}

public enum PayrollRunStatus
{
    Draft,
    Processing,
    Finalized,
    Cancelled,
}

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

    public static Result<PayrollRun> Open(TenantId tenantId, int month, int year, DateTimeOffset occurredOn, string createdBy)
    {
        if (month is < 1 or > 12)
        {
            return Result.Failure<PayrollRun>(Error.Validation("payroll_run.invalid_month", "Month must be between 1 and 12."));
        }

        return Result.Success(new PayrollRun(PayrollRunId.New(), tenantId, month, year, occurredOn, createdBy));
    }

    public Result AddLine(EmployeeId employeeId, Money gross, Money deductions, Money net, decimal lossOfPayDays)
    {
        if (Status != PayrollRunStatus.Draft)
        {
            return Result.Failure(Error.Conflict("payroll_run.not_draft", "Lines can only be added while the run is in Draft."));
        }

        _lines.Add(new PayrollLine(PayrollLineId.New(), employeeId, gross, deductions, net, lossOfPayDays));
        return Result.Success();
    }

    public Result Finalize(DateTimeOffset occurredOn)
    {
        if (Status == PayrollRunStatus.Finalized)
        {
            return Result.Failure(Error.Conflict("payroll_run.already_finalized", "This payroll run has already been finalized."));
        }

        if (_lines.Count == 0)
        {
            return Result.Failure(Error.Validation("payroll_run.empty", "Cannot finalize a payroll run with no lines."));
        }

        Status = PayrollRunStatus.Finalized;
        Raise(new PayrollFinalized(Id, Month, Year, _lines.Count, occurredOn));
        return Result.Success();
    }
}
