using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave.Events;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Leave;

public readonly record struct LeaveRequestId(Guid Value)
{
    public static LeaveRequestId New() => new(Guid.NewGuid());
}

public enum LeaveRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
}

public sealed class LeaveRequest : AggregateRoot<LeaveRequestId>, IAuditable, ITenantScoped
{
    private LeaveRequest(
        LeaveRequestId id, TenantId tenantId, EmployeeId employeeId, LeaveTypeId leaveTypeId, DateRange period,
        decimal requestedDays, string reason, DateTimeOffset createdAt, string createdBy)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        LeaveTypeId = leaveTypeId;
        Period = period;
        RequestedDays = requestedDays;
        Reason = reason;
        Status = LeaveRequestStatus.Pending;
        LossOfPayDays = 0m;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public LeaveTypeId LeaveTypeId { get; }

    public DateRange Period { get; }

    public decimal RequestedDays { get; }

    public string Reason { get; }

    public LeaveRequestStatus Status { get; private set; }

    public decimal LossOfPayDays { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public string CreatedBy { get; }

    public DateTimeOffset? ModifiedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Result<LeaveRequest> Submit(
        TenantId tenantId, EmployeeId employeeId, LeaveTypeId leaveTypeId, DateRange period, decimal requestedDays,
        string reason, DateTimeOffset occurredOn, string createdBy)
    {
        if (requestedDays <= 0)
        {
            return Result.Failure<LeaveRequest>(Error.Validation("leave_request.invalid_days", "Requested days must be positive."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<LeaveRequest>(Error.Validation("leave_request.reason_required", "A reason is required."));
        }

        return Result.Success(new LeaveRequest(
            LeaveRequestId.New(), tenantId, employeeId, leaveTypeId, period, requestedDays, reason.Trim(), occurredOn, createdBy));
    }

    public Result Approve(EmployeeId approverId, DateTimeOffset occurredOn, string approvedBy)
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            return Result.Failure(Error.Conflict("leave_request.not_pending", "Only a pending request can be approved."));
        }

        Status = LeaveRequestStatus.Approved;
        Touch(occurredOn, approvedBy);
        Raise(new LeaveApproved(Id, EmployeeId, Period, approverId, occurredOn));
        return Result.Success();
    }

    public Result Reject(EmployeeId approverId, string reason, DateTimeOffset occurredOn, string rejectedBy)
    {
        if (Status != LeaveRequestStatus.Pending)
        {
            return Result.Failure(Error.Conflict("leave_request.not_pending", "Only a pending request can be rejected."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(Error.Validation("leave_request.rejection_reason_required", "Rejection reason is required."));
        }

        Status = LeaveRequestStatus.Rejected;
        Touch(occurredOn, rejectedBy);
        Raise(new LeaveRejected(Id, EmployeeId, approverId, reason.Trim(), occurredOn));
        return Result.Success();
    }

    public Result Cancel(DateTimeOffset occurredOn, string cancelledBy)
    {
        if (Status is not (LeaveRequestStatus.Pending or LeaveRequestStatus.Approved))
        {
            return Result.Failure(Error.Conflict("leave_request.cannot_cancel", "Only a pending or approved request can be cancelled."));
        }

        Status = LeaveRequestStatus.Cancelled;
        Touch(occurredOn, cancelledBy);
        return Result.Success();
    }

    public Result FlagLossOfPay(decimal lopDays)
    {
        if (lopDays < 0)
        {
            return Result.Failure(Error.Validation("leave_request.invalid_lop", "Loss-of-pay days cannot be negative."));
        }

        LossOfPayDays = lopDays;
        return Result.Success();
    }

    private void Touch(DateTimeOffset occurredOn, string modifiedBy)
    {
        ModifiedAt = occurredOn;
        ModifiedBy = modifiedBy;
    }
}
