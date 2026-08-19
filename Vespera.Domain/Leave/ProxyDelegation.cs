using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Leave;

public readonly record struct ProxyDelegationId(Guid Value)
{
    public static ProxyDelegationId New() => new(Guid.NewGuid());
}

public enum DelegationScope
{
    LeaveApprovals,
    ExpenseApprovals,
    All,
}

public sealed class ProxyDelegation : AggregateRoot<ProxyDelegationId>, ITenantScoped
{
    private ProxyDelegation(
        ProxyDelegationId id, TenantId tenantId, EmployeeId delegatorId, EmployeeId delegateId, DateRange validity, DelegationScope scope)
        : base(id)
    {
        TenantId = tenantId;
        DelegatorId = delegatorId;
        DelegateId = delegateId;
        Validity = validity;
        Scope = scope;
        IsRevoked = false;
    }

    public TenantId TenantId { get; }

    public EmployeeId DelegatorId { get; }

    public EmployeeId DelegateId { get; }

    public DateRange Validity { get; }

    public DelegationScope Scope { get; }

    public bool IsRevoked { get; private set; }

    public static Result<ProxyDelegation> Create(
        TenantId tenantId, EmployeeId delegatorId, EmployeeId delegateId, DateRange validity, DelegationScope scope)
    {
        if (delegatorId == delegateId)
        {
            return Result.Failure<ProxyDelegation>(
                Error.Validation("proxy_delegation.self_delegation", "An employee cannot delegate to themselves."));
        }

        return Result.Success(new ProxyDelegation(ProxyDelegationId.New(), tenantId, delegatorId, delegateId, validity, scope));
    }

    public Result Revoke()
    {
        if (IsRevoked)
        {
            return Result.Failure(Error.Conflict("proxy_delegation.already_revoked", "This delegation has already been revoked."));
        }

        IsRevoked = true;
        return Result.Success();
    }

    public bool IsActiveOn(DateOnly date) => !IsRevoked && Validity.Contains(date);
}
