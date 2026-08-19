using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Domain.UnitTests.Leave;

public class LeaveBalanceTests
{
    [Fact]
    public void Available_Should_Combine_Accrued_CarriedForward_And_Used()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New(), 2026, carriedForward: 2m);
        balance.Accrue(10m);
        balance.Deduct(3m);

        balance.Available.Should().Be(9m);
    }

    [Fact]
    public void Deduct_Should_Reject_A_NonPositive_Amount()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New(), 2026);

        var result = balance.Deduct(0m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Available_Can_Go_Negative_When_Used_Exceeds_Accrued()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New(), 2026);
        balance.Accrue(2m);
        balance.Deduct(5m);

        balance.Available.Should().Be(-3m);
    }
}
