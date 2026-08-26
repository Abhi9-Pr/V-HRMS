using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Domain.UnitTests.Leave;

public class LeaveBalanceTests
{
    private static readonly DateTimeOffset OccurredOn = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Available_Should_Combine_Accrued_CarriedForward_And_Used()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());
        balance.PostEntry(LeaveLedgerEntryType.CarryForward, LeaveLedgerDirection.Credit, 2m, "Carried forward", OccurredOn, "system");
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 10m, "Monthly accrual", OccurredOn, "system");
        balance.PostEntry(LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 3m, "Leave taken", OccurredOn, "system");

        balance.Available.Should().Be(9m);
        balance.Accrued.Should().Be(10m);
        balance.CarriedForward.Should().Be(2m);
        balance.Used.Should().Be(3m);
    }

    [Fact]
    public void PostEntry_Should_Reject_A_NonPositive_Amount()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());

        var result = balance.PostEntry(LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 0m, "Leave taken", OccurredOn, "system");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Available_Can_Go_Negative_When_Used_Exceeds_Accrued()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 2m, "Monthly accrual", OccurredOn, "system");
        balance.PostEntry(LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 5m, "Leave taken", OccurredOn, "system");

        balance.Available.Should().Be(-3m);
    }

    [Fact]
    public void PostEntry_Should_Reject_A_Debit_That_Breaches_The_Negative_Balance_Floor()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 2m, "Monthly accrual", OccurredOn, "system");

        var result = balance.PostEntry(
            LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 5m, "Leave taken", OccurredOn, "system",
            minimumAllowedBalance: 0m);

        result.IsFailure.Should().BeTrue();
        balance.Available.Should().Be(2m);
    }

    [Fact]
    public void Reversal_Should_Restore_The_Balance_After_A_Debit()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());
        balance.PostEntry(LeaveLedgerEntryType.Accrual, LeaveLedgerDirection.Credit, 10m, "Monthly accrual", OccurredOn, "system");
        var debit = balance.PostEntry(
            LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 4m, "Leave taken", OccurredOn, "system",
            sourceType: "LeaveRequest", sourceId: Guid.NewGuid()).Value;

        balance.PostEntry(
            LeaveLedgerEntryType.Reversal, LeaveLedgerDirection.Credit, debit.Amount, "Rejected", OccurredOn, "system",
            sourceType: debit.SourceType, sourceId: debit.SourceId);

        balance.Available.Should().Be(10m);
        balance.Used.Should().Be(0m);
    }
}
