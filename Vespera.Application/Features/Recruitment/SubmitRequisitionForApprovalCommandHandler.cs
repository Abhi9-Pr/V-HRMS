using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Recruitment;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Recruitment;

/// <summary>Transitions the requisition to PendingApproval and opens a single-step
/// <see cref="ApprovalChain"/> against the SUBMITTER's own resolved manager (there is no
/// "department head" concept in this domain model — this is a deliberate simplification, the
/// same reasoning documented on <c>SubmitExpenseClaimCommandHandler</c>) — reusing the same
/// generic approval-chain engine Leave/Expenses use (<see cref="ApprovalSubjectType.JobRequisition"/>).</summary>
public sealed class SubmitRequisitionForApprovalCommandHandler : IRequestHandler<SubmitRequisitionForApprovalCommand, Result>
{
    private readonly IReadRepository<JobRequisition> _requisitionReads;
    private readonly IWriteRepository<JobRequisition> _requisitions;
    private readonly IReadRepository<ReportingRelationship> _reportingRelationships;
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations;
    private readonly IWriteRepository<ApprovalChain> _approvalChains;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public SubmitRequisitionForApprovalCommandHandler(
        IReadRepository<JobRequisition> requisitionReads,
        IWriteRepository<JobRequisition> requisitions,
        IReadRepository<ReportingRelationship> reportingRelationships,
        IReadRepository<ProxyDelegation> proxyDelegations,
        IWriteRepository<ApprovalChain> approvalChains,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        INotificationDispatcher notificationDispatcher,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _requisitionReads = requisitionReads;
        _requisitions = requisitions;
        _reportingRelationships = reportingRelationships;
        _proxyDelegations = proxyDelegations;
        _approvalChains = approvalChains;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _notificationDispatcher = notificationDispatcher;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result> Handle(SubmitRequisitionForApprovalCommand request, CancellationToken cancellationToken)
    {
        var requisition = await _requisitionReads.FirstOrDefaultAsync(
            new JobRequisitionByIdSpecification(new JobRequisitionId(request.RequisitionId)), cancellationToken);

        if (requisition is null)
        {
            return Result.Failure(Error.NotFound("job_requisition.not_found", "Job requisition not found."));
        }

        var currentEmployeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(Error.Validation("job_requisition.no_employee", "The signed-in account is not linked to an employee."));
        }

        var now = _dateTimeProvider.UtcNow;
        var submitResult = requisition.SubmitForApproval(now, currentEmployeeId.Value.Value.ToString());
        if (submitResult.IsFailure)
        {
            return submitResult;
        }

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var manager = await _reportingRelationships.FirstOrDefaultAsync(
            new ActiveReportingRelationshipByEmployeeSpecification(_tenantContext.TenantId, currentEmployeeId.Value, today), cancellationToken);

        if (manager is null)
        {
            return Result.Failure(Error.Validation("job_requisition.no_approver", "No active manager is configured for the submitter."));
        }

        var delegations = await _proxyDelegations.ListAsync(
            new ProxyDelegationsByDelegatorSpecification(_tenantContext.TenantId, manager.ManagerId), cancellationToken);
        var resolvedApproverId = new ApprovalChainResolver().ResolveApprover(manager.ManagerId, today, delegations);

        var chainResult = ApprovalChain.Create(
            _tenantContext.TenantId, ApprovalSubjectType.JobRequisition, requisition.Id.Value, [resolvedApproverId]);
        if (chainResult.IsFailure)
        {
            return chainResult;
        }

        await _approvalChains.AddAsync(chainResult.Value, cancellationToken);
        _requisitions.Update(requisition);

        await _notificationDispatcher.DispatchAsync(
            new NotificationMessage(
                resolvedApproverId.Value.ToString(),
                "Job requisition awaiting your approval",
                $"The requisition '{requisition.Title}' is awaiting your approval.",
                new Dictionary<string, string> { ["jobRequisitionId"] = requisition.Id.Value.ToString() }),
            cancellationToken);

        return Result.Success();
    }
}
