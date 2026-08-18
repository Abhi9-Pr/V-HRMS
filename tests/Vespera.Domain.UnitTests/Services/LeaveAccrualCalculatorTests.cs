using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;

namespace Vespera.Domain.UnitTests.Services;

public class LeaveAccrualCalculatorTests
{
    [Fact]
    public void AccrueForPeriod_Should_Multiply_The_Monthly_Rate_By_The_Number_Of_Months()
    {
        var policy = LeavePolicy.Create(
            TenantId.New(), LeaveTypeId.New(), annualEntitlementDays: 18m, accrualRatePerMonth: 1.5m,
            maxCarryForwardDays: 5m, new DateOnly(2026, 1, 1), null).Value;
        var calculator = new LeaveAccrualCalculator();

        var accrued = calculator.AccrueForPeriod(policy, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        accrued.Should().Be(4.5m);
    }

    [Fact]
    public void AccrueForPeriod_Should_Be_Zero_When_The_Range_Is_Inverted()
    {
        var policy = LeavePolicy.Create(
            TenantId.New(), LeaveTypeId.New(), annualEntitlementDays: 18m, accrualRatePerMonth: 1.5m,
            maxCarryForwardDays: 5m, new DateOnly(2026, 1, 1), null).Value;
        var calculator = new LeaveAccrualCalculator();

        var accrued = calculator.AccrueForPeriod(policy, new DateOnly(2026, 3, 1), new DateOnly(2026, 1, 1));

        accrued.Should().Be(0m);
    }
}
