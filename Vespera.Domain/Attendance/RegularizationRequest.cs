using Vespera.Domain.Attendance.Events;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Attendance;

public readonly record struct RegularizationRequestId(Guid Value)
{
    public static RegularizationRequestId New() => new(Guid.NewGuid());
}

public enum RegularizationStatus
{
    Pending,
    Approved,
    Rejected,
}

public sealed class RegularizationRequest : AggregateRoot<RegularizationRequestId>, ITenantScoped
{
    private RegularizationRequest(
        RegularizationRequestId id,
        TenantId tenantId,
        EmployeeId employeeId,
        AttendanceDayId attendanceDayId,
        string reason,
        string? evidenceFileReference)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        AttendanceDayId = attendanceDayId;
        Reason = reason;
        EvidenceFileReference = evidenceFileReference;
        Status = RegularizationStatus.Pending;
    }

    public TenantId TenantId { get; }

    public EmployeeId EmployeeId { get; }

    public AttendanceDayId AttendanceDayId { get; }

    public string Reason { get; }

    /// <summary>Storage key from <c>IFileStorage</c>, when the employee attached evidence at
    /// submission time. Null when no evidence was provided — evidence is optional.</summary>
    public string? EvidenceFileReference { get; }

    public RegularizationStatus Status { get; private set; }

    public EmployeeId? ApproverId { get; private set; }

    public string? RejectionReason { get; private set; }

    public static Result<RegularizationRequest> Submit(
        TenantId tenantId, EmployeeId employeeId, AttendanceDayId attendanceDayId, string reason, string? evidenceFileReference = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<RegularizationRequest>(
                Error.Validation("regularization_request.reason_required", "A reason is required."));
        }

        return Result.Success(new RegularizationRequest(
            RegularizationRequestId.New(), tenantId, employeeId, attendanceDayId, reason.Trim(), evidenceFileReference));
    }

    public Result Approve(EmployeeId approverId, DateTimeOffset occurredOn)
    {
        if (Status != RegularizationStatus.Pending)
        {
            return Result.Failure(Error.Conflict("regularization_request.not_pending", "Only a pending request can be approved."));
        }

        Status = RegularizationStatus.Approved;
        ApproverId = approverId;
        Raise(new RegularizationApproved(Id, TenantId, EmployeeId, AttendanceDayId, occurredOn));
        return Result.Success();
    }

    public Result Reject(EmployeeId approverId, string reason, DateTimeOffset occurredOn)
    {
        if (Status != RegularizationStatus.Pending)
        {
            return Result.Failure(Error.Conflict("regularization_request.not_pending", "Only a pending request can be rejected."));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(Error.Validation("regularization_request.rejection_reason_required", "Rejection reason is required."));
        }

        Status = RegularizationStatus.Rejected;
        ApproverId = approverId;
        RejectionReason = reason.Trim();
        Raise(new RegularizationRejected(Id, EmployeeId, occurredOn));
        return Result.Success();
    }
}
