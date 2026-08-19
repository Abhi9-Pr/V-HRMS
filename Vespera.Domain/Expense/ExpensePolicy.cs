using Vespera.Domain.Common;
using Vespera.Domain.ValueObjects;

namespace Vespera.Domain.Expense;

public readonly record struct ExpensePolicyId(Guid Value)
{
    public static ExpensePolicyId New() => new(Guid.NewGuid());
}

public sealed class ExpensePolicy : AuditableTenantAggregateRoot<ExpensePolicyId>
{
    private ExpensePolicy(
        ExpensePolicyId id, TenantId tenantId, string category, Money maxAmountPerClaim, Money receiptRequiredAboveAmount,
        DateTimeOffset createdAt, string createdBy)
        : base(id, tenantId, createdAt, createdBy)
    {
        Category = category;
        MaxAmountPerClaim = maxAmountPerClaim;
        ReceiptRequiredAboveAmount = receiptRequiredAboveAmount;
    }

    public string Category { get; private set; }

    public Money MaxAmountPerClaim { get; private set; }

    public Money ReceiptRequiredAboveAmount { get; private set; }

    public static Result<ExpensePolicy> Create(
        TenantId tenantId, string category, Money maxAmountPerClaim, Money receiptRequiredAboveAmount,
        DateTimeOffset occurredOn, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Result.Failure<ExpensePolicy>(Error.Validation("expense_policy.category_required", "Category is required."));
        }

        return Result.Success(new ExpensePolicy(
            ExpensePolicyId.New(), tenantId, category.Trim(), maxAmountPerClaim, receiptRequiredAboveAmount, occurredOn, createdBy));
    }

    public Result UpdateLimits(Money maxAmountPerClaim, Money receiptRequiredAboveAmount, DateTimeOffset occurredOn, string modifiedBy)
    {
        MaxAmountPerClaim = maxAmountPerClaim;
        ReceiptRequiredAboveAmount = receiptRequiredAboveAmount;
        Touch(occurredOn, modifiedBy);
        return Result.Success();
    }
}
