using FluentAssertions;
using Vespera.Application.Features.Leave;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Leave;

namespace Vespera.Application.UnitTests.Features.Leave;

public class LeaveLedgerReverserTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReverseDebit_Should_Post_A_Reversal_For_The_Outstanding_Debit()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());
        var leaveRequestId = LeaveRequestId.New();
        balance.PostEntry(
            LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 4m, "Leave request submitted", Now, "system",
            sourceType: "LeaveRequest", sourceId: leaveRequestId.Value);

        var result = LeaveLedgerReverser.ReverseDebit(balance, leaveRequestId, "Rejected", Now, "system");

        result.IsSuccess.Should().BeTrue();
        balance.Available.Should().Be(0m);
    }

    [Fact]
    public void ReverseDebit_Should_Be_A_NoOp_When_Already_Fully_Reversed()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());
        var leaveRequestId = LeaveRequestId.New();
        balance.PostEntry(
            LeaveLedgerEntryType.Debit, LeaveLedgerDirection.Debit, 4m, "Leave request submitted", Now, "system",
            sourceType: "LeaveRequest", sourceId: leaveRequestId.Value);
        LeaveLedgerReverser.ReverseDebit(balance, leaveRequestId, "Rejected", Now, "system");
        var entryCountAfterFirstReversal = balance.Entries.Count;

        var result = LeaveLedgerReverser.ReverseDebit(balance, leaveRequestId, "Rejected again", Now, "system");

        result.IsSuccess.Should().BeTrue();
        balance.Entries.Should().HaveCount(entryCountAfterFirstReversal);
        balance.Available.Should().Be(0m);
    }

    [Fact]
    public void ReverseDebit_Should_Be_A_NoOp_When_There_Is_No_Matching_Debit()
    {
        var balance = LeaveBalance.Open(TenantId.New(), EmployeeId.New(), LeaveTypeId.New());

        var result = LeaveLedgerReverser.ReverseDebit(balance, LeaveRequestId.New(), "Rejected", Now, "system");

        result.IsSuccess.Should().BeTrue();
        balance.Entries.Should().BeEmpty();
    }
}
