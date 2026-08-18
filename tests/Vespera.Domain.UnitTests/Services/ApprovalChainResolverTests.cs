using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Services;

public class ApprovalChainResolverTests
{
    [Fact]
    public void ResolveApprover_Should_Route_To_The_Delegate_When_A_Proxy_Is_Active()
    {
        var manager = EmployeeId.New();
        var delegateEmployee = EmployeeId.New();
        var validity = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).Value;
        var delegation = ProxyDelegation.Create(
            TenantId.New(), manager, delegateEmployee, validity, DelegationScope.LeaveApprovals).Value;
        var resolver = new ApprovalChainResolver();

        var resolved = resolver.ResolveApprover(manager, new DateOnly(2026, 1, 15), [delegation]);

        resolved.Should().Be(delegateEmployee);
    }

    [Fact]
    public void ResolveApprover_Should_Return_The_Nominal_Approver_When_No_Delegation_Is_Active()
    {
        var manager = EmployeeId.New();
        var delegateEmployee = EmployeeId.New();
        var validity = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).Value;
        var delegation = ProxyDelegation.Create(
            TenantId.New(), manager, delegateEmployee, validity, DelegationScope.LeaveApprovals).Value;
        var resolver = new ApprovalChainResolver();

        var resolved = resolver.ResolveApprover(manager, new DateOnly(2026, 2, 15), [delegation]);

        resolved.Should().Be(manager);
    }

    [Fact]
    public void ResolveApprover_Should_Return_The_Nominal_Approver_When_The_Delegation_Is_Revoked()
    {
        var manager = EmployeeId.New();
        var delegateEmployee = EmployeeId.New();
        var validity = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).Value;
        var delegation = ProxyDelegation.Create(
            TenantId.New(), manager, delegateEmployee, validity, DelegationScope.LeaveApprovals).Value;
        delegation.Revoke();
        var resolver = new ApprovalChainResolver();

        var resolved = resolver.ResolveApprover(manager, new DateOnly(2026, 1, 15), [delegation]);

        resolved.Should().Be(manager);
    }
}
