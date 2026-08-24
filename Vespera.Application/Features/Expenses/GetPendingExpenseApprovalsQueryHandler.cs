using Mapster;
using MediatR;
using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Common;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Expenses;

/// <summary>A genuine cross-aggregate read (ApprovalChain matched against ExpenseClaim by id) —
/// delegation resolution isn't SQL-translatable, so both sides are materialized via their
/// repositories and joined in memory; acceptable for this low-volume, admin-facing list (see
/// CONTRIBUTING-slices.md's persistence-port guidance).</summary>
public sealed class GetPendingExpenseApprovalsQueryHandler : IRequestHandler<GetPendingExpenseApprovalsQuery, Result<PagedResult<ExpenseClaimDto>>>
{
    private readonly IReadRepository<ApprovalChain> _approvalChains;
    private readonly IReadRepository<ExpenseClaim> _expenseClaims;
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public GetPendingExpenseApprovalsQueryHandler(
        IReadRepository<ApprovalChain> approvalChains,
        IReadRepository<ExpenseClaim> expenseClaims,
        IReadRepository<ProxyDelegation> proxyDelegations,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        CurrentEmployeeResolver currentEmployeeResolver)
    {
        _approvalChains = approvalChains;
        _expenseClaims = expenseClaims;
        _proxyDelegations = proxyDelegations;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public async Task<Result<PagedResult<ExpenseClaimDto>>> Handle(
        GetPendingExpenseApprovalsQuery request, CancellationToken cancellationToken)
    {
        var currentEmployeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (currentEmployeeId is null)
        {
            return Result.Failure<PagedResult<ExpenseClaimDto>>(
                Error.Validation("expense_claim.no_employee", "The signed-in account is not linked to an employee."));
        }

        var pendingChains = await _approvalChains.ListAsync(
            new PendingApprovalChainsSpecification(_tenantContext.TenantId, ApprovalSubjectType.ExpenseClaim), cancellationToken);

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var resolver = new ApprovalChainResolver();
        var claimIds = new List<Guid>();

        foreach (var chain in pendingChains)
        {
            var nominalApproverId = chain.CurrentStep.ApproverId;
            var delegations = await _proxyDelegations.ListAsync(
                new ProxyDelegationsByDelegatorSpecification(_tenantContext.TenantId, nominalApproverId), cancellationToken);
            var resolvedApproverId = resolver.ResolveApprover(nominalApproverId, today, delegations);

            if (resolvedApproverId == currentEmployeeId.Value)
            {
                claimIds.Add(chain.SubjectId);
            }
        }

        var tenantClaims = await _expenseClaims.ListAsync(
            new ExpenseClaimsByTenantSpecification(_tenantContext.TenantId), cancellationToken);
        var matchingClaims = tenantClaims.Where(claim => claimIds.Contains(claim.Id.Value)).ToList();

        var page = matchingClaims
            .Skip((request.Paging.Page - 1) * request.Paging.PageSize)
            .Take(request.Paging.PageSize)
            .Adapt<List<ExpenseClaimDto>>();

        return Result.Success(
            new PagedResult<ExpenseClaimDto>(page, request.Paging.Page, request.Paging.PageSize, matchingClaims.Count));
    }
}
