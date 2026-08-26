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
}
