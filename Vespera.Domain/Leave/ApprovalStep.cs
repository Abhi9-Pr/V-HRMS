using Vespera.Domain.Common;
using Vespera.Domain.Eis;

namespace Vespera.Domain.Leave;

public readonly record struct ApprovalStepId(Guid Value)
{
    public static ApprovalStepId New() => new(Guid.NewGuid());
}

public enum ApprovalStepStatus
{
    Pending,
    Approved,
    Rejected,
    Skipped,
}

public sealed class ApprovalStep : Entity<ApprovalStepId>
{
    internal ApprovalStep(ApprovalStepId id, int sequenceNumber, EmployeeId approverId)
        : base(id)
    {
        SequenceNumber = sequenceNumber;
        ApproverId = approverId;
        Status = ApprovalStepStatus.Pending;
    }

    public int SequenceNumber { get; }

    public EmployeeId ApproverId { get; }

    public ApprovalStepStatus Status { get; private set; }

    public EmployeeId? DecidedBy { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public string? Comment { get; private set; }

    public Result Decide(bool approved, EmployeeId decidedBy, DateTimeOffset occurredOn, string? comment)
    {
        if (Status != ApprovalStepStatus.Pending)
        {
            return Result.Failure(Error.Conflict("approval_step.already_decided", "This step has already been decided."));
        }

        Status = approved ? ApprovalStepStatus.Approved : ApprovalStepStatus.Rejected;
        DecidedBy = decidedBy;
        DecidedAt = occurredOn;
        Comment = comment;
        return Result.Success();
    }
}
