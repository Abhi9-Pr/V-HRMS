using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave.Events;

namespace Vespera.Domain.Leave;

public readonly record struct ApprovalChainId(Guid Value)
{
    public static ApprovalChainId New() => new(Guid.NewGuid());
}

public enum ApprovalSubjectType
{
    LeaveRequest,
    ExpenseClaim,
    RegularizationRequest,
    JobRequisition,
}

public enum ApprovalChainStatus
{
    InProgress,
    Approved,
    Rejected,
    Cancelled,
}

public sealed class ApprovalChain : AggregateRoot<ApprovalChainId>, ITenantScoped
{
    private readonly List<ApprovalStep> _steps = [];

    private ApprovalChain(ApprovalChainId id, TenantId tenantId, ApprovalSubjectType subjectType, Guid subjectId)
        : base(id)
    {
        TenantId = tenantId;
        SubjectType = subjectType;
        SubjectId = subjectId;
        Status = ApprovalChainStatus.InProgress;
        CurrentStepIndex = 0;
    }

    public TenantId TenantId { get; }

    public ApprovalSubjectType SubjectType { get; }

    public Guid SubjectId { get; }

    public ApprovalChainStatus Status { get; private set; }

    public int CurrentStepIndex { get; private set; }

    public IReadOnlyList<ApprovalStep> Steps => _steps.AsReadOnly();

    public ApprovalStep CurrentStep => _steps[CurrentStepIndex];

    public static Result<ApprovalChain> Create(
        TenantId tenantId, ApprovalSubjectType subjectType, Guid subjectId, IReadOnlyList<EmployeeId> approverSequence,
        DateTimeOffset occurredOn)
    {
        if (approverSequence is null || approverSequence.Count == 0)
        {
            return Result.Failure<ApprovalChain>(
                Error.Validation("approval_chain.no_steps", "An approval chain needs at least one step."));
        }

        var chain = new ApprovalChain(ApprovalChainId.New(), tenantId, subjectType, subjectId);

        for (var i = 0; i < approverSequence.Count; i++)
        {
            chain._steps.Add(new ApprovalStep(ApprovalStepId.New(), i, approverSequence[i]));
        }

        chain.Raise(new ApprovalStepAssigned(
            chain.Id, tenantId, subjectType, subjectId, chain.CurrentStepIndex, chain.CurrentStep.ApproverId, occurredOn));

        return Result.Success(chain);
    }

    public Result Approve(EmployeeId decidedBy, DateTimeOffset occurredOn, string? comment = null)
    {
        if (Status != ApprovalChainStatus.InProgress)
        {
            return Result.Failure(Error.Conflict("approval_chain.not_in_progress", "This approval chain is no longer in progress."));
        }

        var stepResult = CurrentStep.Decide(approved: true, decidedBy, occurredOn, comment);
        if (stepResult.IsFailure)
        {
            return stepResult;
        }

        if (CurrentStepIndex == _steps.Count - 1)
        {
            Status = ApprovalChainStatus.Approved;
            Raise(new ApprovalChainApproved(Id, TenantId, SubjectType, SubjectId, occurredOn));
        }
        else
        {
            CurrentStepIndex++;
            Raise(new ApprovalStepAssigned(Id, TenantId, SubjectType, SubjectId, CurrentStepIndex, CurrentStep.ApproverId, occurredOn));
        }

        return Result.Success();
    }

    public Result Reject(EmployeeId decidedBy, DateTimeOffset occurredOn, string comment)
    {
        if (Status != ApprovalChainStatus.InProgress)
        {
            return Result.Failure(Error.Conflict("approval_chain.not_in_progress", "This approval chain is no longer in progress."));
        }

        var stepResult = CurrentStep.Decide(approved: false, decidedBy, occurredOn, comment);
        if (stepResult.IsFailure)
        {
            return stepResult;
        }

        Status = ApprovalChainStatus.Rejected;
        Raise(new ApprovalChainRejected(Id, TenantId, SubjectType, SubjectId, decidedBy, comment, occurredOn));
        return Result.Success();
    }

    /// <summary>Withdraws the chain before it reaches a terminal decision — e.g. the requester
    /// withdraws a still-pending leave request. No event is raised: the caller (the command handler
    /// that also cancels the subject aggregate) is the one place that needs to know.</summary>
    public Result Cancel()
    {
        if (Status != ApprovalChainStatus.InProgress)
        {
            return Result.Failure(Error.Conflict("approval_chain.not_in_progress", "This approval chain is no longer in progress."));
        }

        Status = ApprovalChainStatus.Cancelled;
        return Result.Success();
    }
}
