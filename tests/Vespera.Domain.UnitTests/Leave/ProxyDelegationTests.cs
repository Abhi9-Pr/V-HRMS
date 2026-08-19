using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Leave;

public class ProxyDelegationTests
{
    [Fact]
    public void Create_Should_Reject_Self_Delegation()
    {
        var employeeId = EmployeeId.New();
        var validity = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).Value;

        var result = ProxyDelegation.Create(TenantId.New(), employeeId, employeeId, validity, DelegationScope.LeaveApprovals);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void IsActiveOn_Should_Be_True_Within_The_Validity_Window()
    {
        var validity = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).Value;
        var delegation = ProxyDelegation.Create(
            TenantId.New(), EmployeeId.New(), EmployeeId.New(), validity, DelegationScope.LeaveApprovals).Value;

        delegation.IsActiveOn(new DateOnly(2026, 1, 15)).Should().BeTrue();
        delegation.IsActiveOn(new DateOnly(2026, 2, 1)).Should().BeFalse();
    }

    [Fact]
    public void A_Revoked_Delegation_Should_Never_Be_Active()
    {
        var validity = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)).Value;
        var delegation = ProxyDelegation.Create(
            TenantId.New(), EmployeeId.New(), EmployeeId.New(), validity, DelegationScope.LeaveApprovals).Value;

        delegation.Revoke();

        delegation.IsActiveOn(new DateOnly(2026, 1, 15)).Should().BeFalse();
    }
}
