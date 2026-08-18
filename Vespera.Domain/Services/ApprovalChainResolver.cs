using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Domain.Services;

public sealed class ApprovalChainResolver
{
    /// <summary>Returns the active delegate for <paramref name="nominalApproverId"/> on <paramref name="date"/>, else the nominal approver.</summary>
    public EmployeeId ResolveApprover(EmployeeId nominalApproverId, DateOnly date, IEnumerable<ProxyDelegation> delegations)
    {
        ArgumentNullException.ThrowIfNull(delegations);

        var activeDelegation = delegations.FirstOrDefault(
            delegation => delegation.DelegatorId == nominalApproverId && delegation.IsActiveOn(date));

        return activeDelegation?.DelegateId ?? nominalApproverId;
    }
}
