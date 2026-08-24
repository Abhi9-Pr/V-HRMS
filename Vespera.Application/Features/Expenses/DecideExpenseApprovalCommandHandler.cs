using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Domain.Common;
using Vespera.Domain.Expense;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Expenses;

public sealed class DecideExpenseApprovalCommandHandler : IRequestHandler<DecideExpenseApprovalCommand, Result>
{
    private readonly IReadRepository<ApprovalChain> _approvalChains;
    private readonly IReadRepository<ExpenseClaim> _expenseClaims;
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public DecideExpenseApprovalCommandHandler(
        IReadRepository<ApprovalChain> approvalChains,
        IReadRepository<ExpenseClaim> expenseClaims,
        IReadRepository<ProxyDelegation> proxyDelegations,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        INotificationDispatcher notificationDispatcher,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _approvalChains = approvalChains;
        _expenseClaims = expenseClaims;
        _proxyDelegations = proxyDelegations;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _notificationDispatcher = notificationDispatcher;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result> Handle(DecideExpenseApprovalCommand request, CancellationToken cancellationToken)
    {
        var currentEmployeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure(Error.Forbidden("expense_approval.not_authorized", "You are not the current approver for this claim."));
        }

        var chain = await _approvalChains.FirstOrDefaultAsync(
            new ApprovalChainBySubjectSpecification(_tenantContext.TenantId, ApprovalSubjectType.ExpenseClaim, request.ClaimId),
            cancellationToken);

        if (chain is null)
        {
            return Result.Failure(Error.NotFound("expense_claim.approval_not_found", "No pending approval chain was found for this claim."));
        }

        var nominalApproverId = chain.CurrentStep.ApproverId;
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var delegations = await _proxyDelegations.ListAsync(
            new ProxyDelegationsByDelegatorSpecification(_tenantContext.TenantId, nominalApproverId), cancellationToken);
        var resolvedApproverId = new ApprovalChainResolver().ResolveApprover(nominalApproverId, today, delegations);

        if (currentEmployeeId.Value != nominalApproverId && currentEmployeeId.Value != resolvedApproverId)
        {
            return Result.Failure(Error.Forbidden("expense_approval.not_authorized", "You are not the current approver for this claim."));
        }

        var now = _dateTimeProvider.UtcNow;
        var decision = request.Approved
            ? chain.Approve(currentEmployeeId.Value, now, request.Comment)
            : chain.Reject(currentEmployeeId.Value, now, request.Comment ?? "Rejected");

        if (decision.IsFailure)
        {
            return decision;
        }

        var claim = await _expenseClaims.FirstOrDefaultAsync(
            new ExpenseClaimByIdSpecification(new ExpenseClaimId(request.ClaimId)), cancellationToken);

        if (claim is null)
        {
            return Result.Failure(Error.NotFound("expense_claim.not_found", "Expense claim not found."));
        }

        Result claimTransitionResult = chain.Status switch
        {
            ApprovalChainStatus.Approved => claim.Approve(),
            ApprovalChainStatus.Rejected => claim.Reject(request.Comment ?? "Rejected"),
            _ => Result.Success(),
        };

        if (claimTransitionResult.IsFailure)
        {
            return claimTransitionResult;
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationMessage(
                claim.EmployeeId.Value.ToString(),
                request.Approved ? "Your expense claim was approved" : "Your expense claim was rejected",
                request.Comment ?? string.Empty,
                new Dictionary<string, string> { ["expenseClaimId"] = claim.Id.Value.ToString() }),
            cancellationToken);

        return Result.Success();
    }
}
