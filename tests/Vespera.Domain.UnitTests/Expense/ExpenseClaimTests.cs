using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Expense;

public class ExpenseClaimTests
{
    [Fact]
    public void Submit_Should_Fail_With_No_Lines()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());

        var result = claim.Submit();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Submit_Then_Approve_Should_Change_Status()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);

        claim.Submit().IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Submitted);

        claim.Approve().IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Approved);
    }

    [Fact]
    public void MarkReimbursed_Should_Fail_Before_Approval()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        claim.Submit();

        var result = claim.MarkReimbursed();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Total_Should_Sum_All_Lines_In_The_Given_Currency()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        claim.AddLine("Meals", Money.Of(500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);

        claim.Total(Currency.Inr).Should().Be(Money.Of(2000m, Currency.Inr));
    }
}
