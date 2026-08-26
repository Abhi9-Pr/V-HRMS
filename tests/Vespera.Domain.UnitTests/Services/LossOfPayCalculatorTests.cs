using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;
using Vespera.Domain.Services;

namespace Vespera.Domain.UnitTests.Services;

public class LossOfPayCalculatorTests
{
    private static readonly DateTimeOffset OccurredOn = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CalculateLopDays_Should_Be_Zero_When_The_Request_Is_Within_Balance()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 10m, "Monthly accrual", OccurredOn, "system");
        var calculator = new LossOfPayCalculator();

        calculator.CalculateLopDays(balance, requestedDays: 5m).Should().Be(0m);
    }

    [Fact]
    public void CalculateLopDays_Should_Flag_The_Excess_When_The_Request_Exceeds_Balance()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 3m, "Monthly accrual", OccurredOn, "system");
        var calculator = new LossOfPayCalculator();

        calculator.CalculateLopDays(balance, requestedDays: 5m).Should().Be(2m);
    }

    [Fact]
    public void CalculateLopDays_Should_Be_Zero_When_Balance_Exactly_Covers_The_Request()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 5m, "Monthly accrual", OccurredOn, "system");
        var calculator = new LossOfPayCalculator();

        calculator.CalculateLopDays(balance, requestedDays: 5m).Should().Be(0m);
    }
}
