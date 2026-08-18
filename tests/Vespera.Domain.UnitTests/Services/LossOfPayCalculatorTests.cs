using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;

namespace Vespera.Domain.UnitTests.Services;

public class LossOfPayCalculatorTests
{
    [Fact]
    public void CalculateLopDays_Should_Be_Zero_When_The_Request_Is_Within_Balance()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New(), 2026);
        balance.Accrue(10m);
        var calculator = new LossOfPayCalculator();

        calculator.CalculateLopDays(balance, requestedDays: 5m).Should().Be(0m);
    }

    [Fact]
    public void CalculateLopDays_Should_Flag_The_Excess_When_The_Request_Exceeds_Balance()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New(), 2026);
        balance.Accrue(3m);
        var calculator = new LossOfPayCalculator();

        calculator.CalculateLopDays(balance, requestedDays: 5m).Should().Be(2m);
    }

    [Fact]
    public void CalculateLopDays_Should_Be_Zero_When_Balance_Exactly_Covers_The_Request()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New(), 2026);
        balance.Accrue(5m);
        var calculator = new LossOfPayCalculator();

        calculator.CalculateLopDays(balance, requestedDays: 5m).Should().Be(0m);
    }
}
