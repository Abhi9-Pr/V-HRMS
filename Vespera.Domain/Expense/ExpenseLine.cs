using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Expense;

public readonly record struct ExpenseLineId(Guid Value)
{
    public static ExpenseLineId New() => new(Guid.NewGuid());
}

public sealed class ExpenseLine : Entity<ExpenseLineId>
{
    internal ExpenseLine(
        ExpenseLineId id, string category, Money amount, DateOnly expenseDate, string? receiptReference,
        string? vendor = null, Money? taxAmount = null, Money? convertedAmount = null, decimal? exchangeRate = null)
        : base(id)
    {
        Category = category;
        Amount = amount;
        ExpenseDate = expenseDate;
        ReceiptReference = receiptReference;
        Vendor = vendor;
        TaxAmount = taxAmount;
        ConvertedAmount = convertedAmount;
        ExchangeRate = exchangeRate;
    }

    public string Category { get; }

    public Money Amount { get; }

    public DateOnly ExpenseDate { get; }

    public string? ReceiptReference { get; private set; }

    public string? Vendor { get; }

    public Money? TaxAmount { get; }

    /// <summary>The line's amount converted into the claim's <see cref="ExpenseClaim.SettlementCurrency"/>
    /// using <see cref="ExchangeRate"/>, or null when <see cref="Amount"/> is already in that currency.</summary>
    public Money? ConvertedAmount { get; }

    public decimal? ExchangeRate { get; }

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
