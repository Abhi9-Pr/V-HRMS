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

    [Fact]
    public void AddLine_Should_Store_Vendor_Tax_And_Conversion_Details()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);

        var result = claim.AddLine(
            "Travel", Money.Of(100m, Currency.Usd), new DateOnly(2026, 1, 10), receiptReference: "receipt-1",
            vendor: "Uber", taxAmount: Money.Of(5m, Currency.Usd), convertedAmount: Money.Of(8300m, Currency.Inr), exchangeRate: 83m);

        result.IsSuccess.Should().BeTrue();
        var line = claim.Lines.Single();
        line.Vendor.Should().Be("Uber");
        line.TaxAmount.Should().Be(Money.Of(5m, Currency.Usd));
        line.ConvertedAmount.Should().Be(Money.Of(8300m, Currency.Inr));
        line.ExchangeRate.Should().Be(83m);
    }

    [Fact]
    public void AddLine_Should_Fail_When_Converted_Amount_Given_Without_A_Positive_Rate()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);

        var result = claim.AddLine(
            "Travel", Money.Of(100m, Currency.Usd), new DateOnly(2026, 1, 10), receiptReference: null,
            convertedAmount: Money.Of(8300m, Currency.Inr), exchangeRate: 0m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Total_Should_Prefer_Converted_Amount_When_Present()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New(), Currency.Inr);
        claim.AddLine(
            "Travel", Money.Of(100m, Currency.Usd), new DateOnly(2026, 1, 10), receiptReference: null,
            convertedAmount: Money.Of(8300m, Currency.Inr), exchangeRate: 83m);
        claim.AddLine("Meals", Money.Of(500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);

        claim.Total(Currency.Inr).Should().Be(Money.Of(8800m, Currency.Inr));
    }

    [Fact]
    public void AddLine_Should_Fail_Once_The_Claim_Is_No_Longer_Draft()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        claim.Submit();

        var result = claim.AddLine("Meals", Money.Of(500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.not_draft");
    }

    [Fact]
    public void AddLine_Should_Fail_With_A_Blank_Category()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());

        var result = claim.AddLine("  ", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.category_required");
    }

    [Fact]
    public void Submit_Should_Fail_Once_Already_Submitted()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        claim.Submit();

        var result = claim.Submit();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.not_draft");
    }

    [Fact]
    public void Approve_Should_Fail_Before_Submission()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());

        var result = claim.Approve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.not_submitted");
    }

    [Fact]
    public void Reject_Should_Set_The_Reason_And_Status()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        claim.Submit();

        var result = claim.Reject("Missing receipt");

        result.IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Rejected);
        claim.RejectionReason.Should().Be("Missing receipt");
    }

    [Fact]
    public void Reject_Should_Fail_Before_Submission()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());

        var result = claim.Reject("Missing receipt");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.not_submitted");
    }

    [Fact]
    public void Reject_Should_Fail_With_A_Blank_Reason()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        claim.Submit();

        var result = claim.Reject("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_claim.rejection_reason_required");
    }

    [Fact]
    public void MarkReimbursed_Should_Succeed_Once_Approved()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        claim.Submit();
        claim.Approve();

        var result = claim.MarkReimbursed();

        result.IsSuccess.Should().BeTrue();
        claim.Status.Should().Be(ExpenseClaimStatus.Reimbursed);
    }

    [Fact]
    public void ExpenseClaimId_New_Should_Generate_Distinct_Values()
    {
        ExpenseClaimId.New().Should().NotBe(ExpenseClaimId.New());
    }
}
