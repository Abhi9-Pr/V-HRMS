using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;
using Vespera.Domain.Recruitment;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Recruitment;

public sealed class DecideRequisitionApprovalCommandHandler : IRequestHandler<DecideRequisitionApprovalCommand, Result>
{
    private readonly IReadRepository<ApprovalChain> _approvalChains;
    private readonly IReadRepository<JobRequisition> _requisitionReads;
    private readonly IWriteRepository<JobRequisition> _requisitions;
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public DecideRequisitionApprovalCommandHandler(
        IReadRepository<ApprovalChain> approvalChains,
        IReadRepository<JobRequisition> requisitionReads,
        IWriteRepository<JobRequisition> requisitions,
        IReadRepository<ProxyDelegation> proxyDelegations,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        INotificationDispatcher notificationDispatcher,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _approvalChains = approvalChains;
        _requisitionReads = requisitionReads;
        _requisitions = requisitions;
        _proxyDelegations = proxyDelegations;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _notificationDispatcher = notificationDispatcher;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result> Handle(DecideRequisitionApprovalCommand request, CancellationToken cancellationToken)
    {
        var currentEmployeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(Error.Forbidden("requisition_approval.not_authorized", "You are not the current approver for this requisition."));
        }

        var chain = await _approvalChains.FirstOrDefaultAsync(
            new ApprovalChainBySubjectSpecification(_tenantContext.TenantId, ApprovalSubjectType.JobRequisition, request.RequisitionId),
            cancellationToken);

        if (chain is null)
        {
            return Result.Failure(Error.NotFound("job_requisition.approval_not_found", "No pending approval chain was found for this requisition."));
        }

        var nominalApproverId = chain.CurrentStep.ApproverId;
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var delegations = await _proxyDelegations.ListAsync(
            new ProxyDelegationsByDelegatorSpecification(_tenantContext.TenantId, nominalApproverId), cancellationToken);
        var resolvedApproverId = new ApprovalChainResolver().ResolveApprover(nominalApproverId, today, delegations);

        if (currentEmployeeId.Value != nominalApproverId && currentEmployeeId.Value != resolvedApproverId)
        {
            return Result.Failure(Error.Forbidden("requisition_approval.not_authorized", "You are not the current approver for this requisition."));
        }

        var now = _dateTimeProvider.UtcNow;
        var decision = request.Approved
            ? chain.Approve(currentEmployeeId.Value, now, request.Comment)
            : chain.Reject(currentEmployeeId.Value, now, request.Comment ?? "Rejected");

        if (decision.IsFailure)
        {
            return decision;
        }

        var requisition = await _requisitionReads.FirstOrDefaultAsync(
            new JobRequisitionByIdSpecification(new JobRequisitionId(request.RequisitionId)), cancellationToken);

        if (requisition is null)
        {
            return Result.Failure(Error.NotFound("job_requisition.not_found", "Job requisition not found."));
        }

        var modifiedBy = currentEmployeeId.Value.Value.ToString();
        Result transitionResult = chain.Status switch
        {
            ApprovalChainStatus.Approved => requisition.ApproveRequisition(now, modifiedBy),
            ApprovalChainStatus.Rejected => requisition.RejectRequisition(request.Comment ?? "Rejected", now, modifiedBy),
            _ => Result.Success(),
        };

        if (transitionResult.IsFailure)
        {
            return transitionResult;
        }

        _requisitions.Update(requisition);

        await _notificationDispatcher.DispatchAsync(
            new NotificationMessage(
                requisition.CreatedBy,
                request.Approved ? "Your job requisition was approved" : "Your job requisition was rejected",
                request.Comment ?? string.Empty,
                new Dictionary<string, string> { ["jobRequisitionId"] = requisition.Id.Value.ToString() }),
            cancellationToken);

        return Result.Success();
    }
}
