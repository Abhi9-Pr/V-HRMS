using Vespera.Application.Abstractions.Identity;
using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Abstractions.Services;
using Vespera.Application.Features.Expenses;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;
using Vespera.Domain.Workspace;

namespace Vespera.Application.Features.Dashboard.Widgets;

public sealed record PendingApprovalsWidgetDto(int TotalCount, IReadOnlyDictionary<string, int> CountBySubjectType);

/// <summary>Counts every approval chain (across leave, expenses, regularizations, and
/// requisitions — see <see cref="ApprovalSubjectType"/>) currently sitting with the caller as the
/// resolved approver, generalizing the same delegation-aware resolution
/// <c>GetPendingExpenseApprovalsQueryHandler</c> already does for one subject type.</summary>
public sealed class PendingApprovalsWidgetProvider : IDashboardWidgetProvider
{
    private readonly IReadRepository<ApprovalChain> _approvalChains;
    private readonly IReadRepository<ProxyDelegation> _proxyDelegations;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CurrentEmployeeResolver _currentEmployeeResolver;

    public PendingApprovalsWidgetProvider(
        IReadRepository<ApprovalChain> approvalChains, IReadRepository<ProxyDelegation> proxyDelegations, ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider, CurrentEmployeeResolver currentEmployeeResolver)
    {
        _approvalChains = approvalChains;
        _proxyDelegations = proxyDelegations;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentEmployeeResolver = currentEmployeeResolver;
    }

    public string WidgetKey => "pendingApprovals";

    public int DefaultOrder => 5;

    public bool DefaultVisible => true;

    public WidgetSize DefaultSize => WidgetSize.Small;

    public async Task<Result<object?>> GetPayloadAsync(CancellationToken cancellationToken)
    {
        var employeeId = await _currentEmployeeResolver.ResolveAsync(cancellationToken);
        if (employeeId is null)
        {
            return Result.Success<object?>(new PendingApprovalsWidgetDto(0, new Dictionary<string, int>()));
        }

        var tenantId = _tenantContext.TenantId;
        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var chains = await _approvalChains.ListAsync(new InProgressApprovalChainsSpecification(tenantId), cancellationToken);
        var resolver = new ApprovalChainResolver();

        var countBySubjectType = new Dictionary<string, int>();
        foreach (var chain in chains)
        {
            var nominalApproverId = chain.CurrentStep.ApproverId;
            var delegations = await _proxyDelegations.ListAsync(
                new ProxyDelegationsByDelegatorSpecification(tenantId, nominalApproverId), cancellationToken);
            var resolvedApproverId = resolver.ResolveApprover(nominalApproverId, today, delegations);

            if (resolvedApproverId == employeeId.Value)
            {
                var key = chain.SubjectType.ToString();
                countBySubjectType[key] = countBySubjectType.GetValueOrDefault(key) + 1;
            }
        }

        return Result.Success<object?>(new PendingApprovalsWidgetDto(countBySubjectType.Values.Sum(), countBySubjectType));
    }
}
