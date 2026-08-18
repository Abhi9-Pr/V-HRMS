using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Expense;

public readonly record struct ExpenseLineId(Guid Value)
{
    public static ExpenseLineId New() => new(Guid.NewGuid());
}

public sealed class ExpenseLine : Entity<ExpenseLineId>
{
    internal ExpenseLine(ExpenseLineId id, string category, Money amount, DateOnly expenseDate, string? receiptReference)
        : base(id)
    {
        Category = category;
        Amount = amount;
        ExpenseDate = expenseDate;
        ReceiptReference = receiptReference;
    }

    public string Category { get; }

    public Money Amount { get; }

    public DateOnly ExpenseDate { get; }

    public string? ReceiptReference { get; private set; }

    public Result AttachReceipt(string receiptReference)
    {
        if (string.IsNullOrWhiteSpace(receiptReference))
        {
            return Result.Failure(Error.Validation("expense_line.receipt_reference_required", "Receipt reference is required."));
        }

        ReceiptReference = receiptReference.Trim();
        return Result.Success();
    }
}
