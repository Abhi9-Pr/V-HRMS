using Vespera.Application.Abstractions.Persistence;
using Vespera.Application.Features.Attendance.Regularizations;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;

namespace Vespera.Application.Features.Leave;

/// <summary>Resolves who is actually authorized to decide an <see cref="ApprovalChain"/>'s
/// <see cref="ApprovalChain.CurrentStep"/> — the step's nominal <see cref="ApprovalStep.ApproverId"/>,
/// unless that person has an active <see cref="DelegationScope.LeaveApprovals"/> (or <c>All</c>)
/// <see cref="ProxyDelegation"/> covering today, in which case the delegate decides instead. Called
/// fresh on every approve/reject — not baked in when the chain is built — so a delegation activated
/// after submission still applies to an already-pending step, exactly like "Holiday Mode" is
/// supposed to.</summary>
public sealed class LeaveApprovalStepAuthorizer
{
    private readonly IReadRepository<ProxyDelegation> _delegations;
    private readonly ApprovalChainResolver _approvalChainResolver = new();

    public LeaveApprovalStepAuthorizer(IReadRepository<ProxyDelegation> delegations)
    {
        _delegations = delegations;
    }

    public async Task<EmployeeId> ResolveAuthorizedApproverAsync(EmployeeId nominalApproverId, DateOnly asOf, CancellationToken cancellationToken)
    {
        var delegations = (await _delegations.ListAsync(new DelegationsByDelegatorSpecification(nominalApproverId), cancellationToken))
            .Where(d => d.IsActiveOn(asOf) && d.Scope is DelegationScope.LeaveApprovals or DelegationScope.All)
            .ToList();

        return _approvalChainResolver.ResolveApprover(nominalApproverId, asOf, delegations);
    }
}
