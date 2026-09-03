using FluentAssertions;
using Vespera.Domain.Common;
using Vespera.Domain.Eis;
using Vespera.Domain.Expense;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.UnitTests.Expense;

public class ExpenseLineTests
{
    [Fact]
    public void AddLine_Should_Create_A_Line_With_The_Given_Details()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());

        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: "receipt-1");

        var line = claim.Lines.Single();
        line.Category.Should().Be("Travel");
        line.Amount.Should().Be(Money.Of(1500m, Currency.Inr));
        line.ExpenseDate.Should().Be(new DateOnly(2026, 1, 10));
        line.ReceiptReference.Should().Be("receipt-1");
        line.Vendor.Should().BeNull();
        line.TaxAmount.Should().BeNull();
        line.ConvertedAmount.Should().BeNull();
        line.ExchangeRate.Should().BeNull();
    }

    [Fact]
    public void AttachReceipt_Should_Set_The_Reference()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        var line = claim.Lines.Single();

        var result = line.AttachReceipt("receipt-42");

        result.IsSuccess.Should().BeTrue();
        line.ReceiptReference.Should().Be("receipt-42");
    }

    [Fact]
    public void AttachReceipt_Should_Fail_With_A_Blank_Reference()
    {
        var claim = ExpenseClaim.Open(TenantId.New(), EmployeeId.New());
        claim.AddLine("Travel", Money.Of(1500m, Currency.Inr), new DateOnly(2026, 1, 10), receiptReference: null);
        var line = claim.Lines.Single();

        var result = line.AttachReceipt("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expense_line.receipt_reference_required");
    }

    [Fact]
    public void ExpenseLineId_New_Should_Generate_Distinct_Values()
    {
        ExpenseLineId.New().Should().NotBe(ExpenseLineId.New());
    }
}
